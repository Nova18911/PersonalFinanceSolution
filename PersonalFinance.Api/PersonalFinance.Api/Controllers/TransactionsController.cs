using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions()
        {
            return await _context.Transactions
                                 .Include(t => t.ExpenseItem)
                                 .ThenInclude(e => e.Category)
                                 .OrderByDescending(t => t.Date)
                                 .ToListAsync();
        }

        // GET: api/Transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.ExpenseItem)
                .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return NotFound();
            return transaction;
        }

        // GET: api/Transactions/byDate/{date}
        [HttpGet("byDate/{date}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetByDate(DateOnly date)
        {
            return await _context.Transactions
                .Where(t => t.Date == date)
                .Include(t => t.ExpenseItem)
                .ThenInclude(e => e.Category)
                .ToListAsync();
        }

        // GET: api/Transactions/byMonth/{year}/{month}
        [HttpGet("byMonth/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetByMonth(int year, int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return await _context.Transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .Include(t => t.ExpenseItem)
                .ThenInclude(e => e.Category)
                .ToListAsync();
        }

        // POST: api/Transactions
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            // Проверяем, существует ли статья и активна ли она
            var expenseItem = await _context.ExpenseItems
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == transaction.ExpenseItemId);

            if (expenseItem == null)
                return BadRequest("Статья расхода не найдена.");

            if (!expenseItem.IsActive)
                return BadRequest("Нельзя использовать неактивную статью расхода.");

            // ИСПРАВЛЕНО: считаем сумму всех транзакций за день (без фильтра по статье)
            var totalToday = await _context.Transactions
                .Where(t => t.Date == transaction.Date)
                .SumAsync(t => t.Amount);

            if (totalToday + transaction.Amount > 1_000_000)
                return BadRequest($"Превышен дневной лимит в 1 000 000 рублей. Сегодня уже потрачено: {totalToday:F2} руб.");

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для ответа
            await _context.Entry(transaction).Reference(t => t.ExpenseItem).LoadAsync();
            await _context.Entry(transaction.ExpenseItem).Reference(e => e.Category).LoadAsync();

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        // PUT: api/Transactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction)
        {
            if (id != transaction.Id) return BadRequest();

            var existing = await _context.Transactions.FindAsync(id);
            if (existing == null) return NotFound();

            // Если статья меняется — проверяем, что новая статья активна
            if (existing.ExpenseItemId != transaction.ExpenseItemId)
            {
                var newItem = await _context.ExpenseItems.FindAsync(transaction.ExpenseItemId);
                if (newItem == null)
                    return BadRequest("Статья расхода не найдена.");
                if (!newItem.IsActive)
                    return BadRequest("Нельзя изменить статью на неактивную.");
            }

            // Проверяем дневной лимит без учёта текущей транзакции
            var totalToday = await _context.Transactions
                .Where(t => t.Date == transaction.Date && t.Id != id)
                .SumAsync(t => t.Amount);

            if (totalToday + transaction.Amount > 1_000_000)
                return BadRequest($"Превышен дневной лимит. Доступно: {1_000_000 - totalToday:F2} руб.");

            existing.Date = transaction.Date;
            existing.Amount = transaction.Amount;
            existing.Comment = transaction.Comment;
            existing.ExpenseItemId = transaction.ExpenseItemId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Transactions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}