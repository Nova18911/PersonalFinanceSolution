using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpenseItemsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ExpenseItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseItem>>> GetExpenseItems()
        {
            return await _context.ExpenseItems
                                 .Include(e => e.Category)
                                 .ToListAsync();
        }

        // GET: api/ExpenseItems/byCategory/5
        [HttpGet("byCategory/{categoryId}")]
        public async Task<ActionResult<IEnumerable<ExpenseItem>>> GetByCategory(int categoryId)
        {
            return await _context.ExpenseItems
                                 .Where(e => e.CategoryId == categoryId)
                                 .Include(e => e.Category)
                                 .ToListAsync();
        }

        // GET: api/ExpenseItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseItem>> GetExpenseItem(int id)
        {
            var item = await _context.ExpenseItems
                                     .Include(e => e.Category)
                                     .FirstOrDefaultAsync(e => e.Id == id);

            if (item == null) return NotFound();

            return item;
        }

        // POST: api/ExpenseItems
        [HttpPost]
        public async Task<ActionResult<ExpenseItem>> CreateExpenseItem(ExpenseItem expenseItem)
        {
            // Проверяем, существует ли категория
            if (!await _context.Categories.AnyAsync(c => c.Id == expenseItem.CategoryId))
                return BadRequest("Указанная категория не существует.");

            _context.ExpenseItems.Add(expenseItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExpenseItem), new { id = expenseItem.Id }, expenseItem);
        }

        // PUT: api/ExpenseItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpenseItem(int id, ExpenseItem expenseItem)
        {
            if (id != expenseItem.Id) return BadRequest();

            _context.Entry(expenseItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExpenseItemExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/ExpenseItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpenseItem(int id)
        {
            var item = await _context.ExpenseItems
                                     .Include(e => e.Transactions)
                                     .FirstOrDefaultAsync(e => e.Id == id);

            if (item == null) return NotFound();

            if (item.Transactions.Any())
                return BadRequest("Нельзя удалить статью, по которой есть транзакции.");

            _context.ExpenseItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ExpenseItemExists(int id)
        {
            return _context.ExpenseItems.Any(e => e.Id == id);
        }
    }
}