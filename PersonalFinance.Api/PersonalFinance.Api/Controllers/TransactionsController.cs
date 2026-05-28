using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers
{
    /// <summary>
    /// Контроллер для управления транзакциями (расходными операциями).
    /// Предоставляет REST API для получения, создания, обновления и удаления транзакций.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Инициализирует контроллер с контекстом базы данных.
        /// </summary>
        /// <param name="context">Контекст базы данных, предоставляемый через внедрение зависимостей.</param>
        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает список всех транзакций с вложенными данными статей расходов и категорий,
        /// отсортированный по убыванию даты.
        /// </summary>
        /// <returns>Коллекция всех транзакций.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions()
        {
            return await _context.Transactions
                                 .Include(t => t.ExpenseItem)
                                 .ThenInclude(e => e.Category)
                                 .OrderByDescending(t => t.Date)
                                 .ToListAsync();
        }

        /// <summary>
        /// Возвращает транзакцию по указанному идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор транзакции.</param>
        /// <returns>Транзакция или 404, если не найдена.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.ExpenseItem)
                .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return NotFound();

            return transaction;
        }

        /// <summary>
        /// Возвращает список транзакций за указанную дату.
        /// </summary>
        /// <param name="date">Дата в формате yyyy-MM-dd.</param>
        /// <returns>Коллекция транзакций за указанный день.</returns>
        [HttpGet("byDate/{date}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetByDate(DateOnly date)
        {
            return await _context.Transactions
                .Where(t => t.Date == date)
                .Include(t => t.ExpenseItem)
                .ThenInclude(e => e.Category)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает список транзакций за указанный месяц.
        /// </summary>
        /// <param name="year">Год (например, 2026).</param>
        /// <param name="month">Месяц от 1 до 12.</param>
        /// <returns>Коллекция транзакций за указанный месяц.</returns>
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

        /// <summary>
        /// Создаёт новую транзакцию.
        /// При создании проверяется активность статьи расходов и дневной лимит в 1 000 000 рублей.
        /// </summary>
        /// <param name="transaction">Данные создаваемой транзакции.</param>
        /// <returns>Созданная транзакция с присвоенным идентификатором.</returns>
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            // Проверяем существование и активность выбранной статьи расходов
            var expenseItem = await _context.ExpenseItems
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == transaction.ExpenseItemId);

            if (expenseItem == null)
                return BadRequest("Статья расхода не найдена.");

            if (!expenseItem.IsActive)
                return BadRequest("Нельзя использовать неактивную статью расхода.");

            // Проверяем, что суммарные расходы за день не превысят 1 000 000 рублей
            var totalToday = await _context.Transactions
                .Where(t => t.Date == transaction.Date)
                .SumAsync(t => t.Amount);

            if (totalToday + transaction.Amount > 1_000_000)
                return BadRequest($"Превышен дневной лимит в 1 000 000 рублей. Сегодня уже потрачено: {totalToday:F2} руб.");

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для формирования полного ответа
            await _context.Entry(transaction).Reference(t => t.ExpenseItem).LoadAsync();
            await _context.Entry(transaction.ExpenseItem).Reference(e => e.Category).LoadAsync();

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        /// <summary>
        /// Обновляет существующую транзакцию.
        /// Запрещено изменять статью расходов на неактивную.
        /// При изменении суммы или даты повторно проверяется дневной лимит.
        /// </summary>
        /// <param name="id">Идентификатор обновляемой транзакции.</param>
        /// <param name="transaction">Новые данные транзакции. Поле Id должно совпадать с параметром id.</param>
        /// <returns>204 NoContent при успехе, 400 или 404 при ошибке.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction)
        {
            if (id != transaction.Id)
                return BadRequest();

            var existing = await _context.Transactions.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Если статья расходов изменяется — проверяем, что новая статья активна
            if (existing.ExpenseItemId != transaction.ExpenseItemId)
            {
                var newItem = await _context.ExpenseItems.FindAsync(transaction.ExpenseItemId);
                if (newItem == null)
                    return BadRequest("Статья расхода не найдена.");
                if (!newItem.IsActive)
                    return BadRequest("Нельзя изменить статью на неактивную.");
            }

            // Проверяем дневной лимит без учёта текущей редактируемой транзакции
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

        /// <summary>
        /// Удаляет транзакцию по указанному идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой транзакции.</param>
        /// <returns>204 NoContent при успехе, 404 если транзакция не найдена.</returns>
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