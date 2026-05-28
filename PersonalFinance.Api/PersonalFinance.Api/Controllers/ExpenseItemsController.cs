using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers
{
    /// <summary>
    /// Контроллер для управления статьями расходов.
    /// Предоставляет REST API для получения, создания, обновления и удаления статей расходов.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Инициализирует контроллер с контекстом базы данных.
        /// </summary>
        /// <param name="context">Контекст базы данных, предоставляемый через внедрение зависимостей.</param>
        public ExpenseItemsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает список всех статей расходов вместе со связанными категориями.
        /// </summary>
        /// <returns>Коллекция статей расходов.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseItem>>> GetExpenseItems()
        {
            return await _context.ExpenseItems
                                 .Include(e => e.Category)
                                 .ToListAsync();
        }

        /// <summary>
        /// Возвращает список статей расходов, принадлежащих указанной категории.
        /// </summary>
        /// <param name="categoryId">Идентификатор категории.</param>
        /// <returns>Коллекция статей расходов в рамках указанной категории.</returns>
        [HttpGet("byCategory/{categoryId}")]
        public async Task<ActionResult<IEnumerable<ExpenseItem>>> GetByCategory(int categoryId)
        {
            return await _context.ExpenseItems
                                 .Where(e => e.CategoryId == categoryId)
                                 .Include(e => e.Category)
                                 .ToListAsync();
        }

        /// <summary>
        /// Возвращает статью расходов по указанному идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор статьи расходов.</param>
        /// <returns>Статья расходов или 404, если не найдена.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseItem>> GetExpenseItem(int id)
        {
            var item = await _context.ExpenseItems
                                     .Include(e => e.Category)
                                     .FirstOrDefaultAsync(e => e.Id == id);
            if (item == null)
                return NotFound();

            return item;
        }

        /// <summary>
        /// Создаёт новую статью расходов и привязывает её к существующей категории.
        /// </summary>
        /// <param name="expenseItem">Данные создаваемой статьи расходов.</param>
        /// <returns>Созданная статья расходов с присвоенным идентификатором.</returns>
        [HttpPost]
        public async Task<ActionResult<ExpenseItem>> CreateExpenseItem(ExpenseItem expenseItem)
        {
            // Проверяем существование указанной категории перед созданием статьи
            if (!await _context.Categories.AnyAsync(c => c.Id == expenseItem.CategoryId))
                return BadRequest("Указанная категория не существует.");

            _context.ExpenseItems.Add(expenseItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExpenseItem), new { id = expenseItem.Id }, expenseItem);
        }

        /// <summary>
        /// Обновляет существующую статью расходов.
        /// </summary>
        /// <param name="id">Идентификатор обновляемой статьи расходов.</param>
        /// <param name="expenseItem">Новые данные статьи. Поле Id должно совпадать с параметром id.</param>
        /// <returns>204 NoContent при успехе, 400 или 404 при ошибке.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpenseItem(int id, ExpenseItem expenseItem)
        {
            if (id != expenseItem.Id)
                return BadRequest();

            _context.Entry(expenseItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExpenseItemExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Удаляет статью расходов по указанному идентификатору.
        /// Удаление запрещено, если по статье существуют транзакции.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой статьи расходов.</param>
        /// <returns>204 NoContent при успехе, 400 если удаление запрещено, 404 если не найдена.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpenseItem(int id)
        {
            var item = await _context.ExpenseItems
                                     .Include(e => e.Transactions)
                                     .FirstOrDefaultAsync(e => e.Id == id);

            if (item == null)
                return NotFound();

            // Запрещаем удаление статьи расходов при наличии связанных транзакций
            if (item.Transactions.Any())
                return BadRequest("Нельзя удалить статью, по которой есть транзакции.");

            _context.ExpenseItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Проверяет существование статьи расходов с указанным идентификатором.
        /// </summary>
        /// <param name="id">Идентификатор статьи расходов.</param>
        /// <returns>True, если статья существует; иначе false.</returns>
        private bool ExpenseItemExists(int id)
        {
            return _context.ExpenseItems.Any(e => e.Id == id);
        }
    }
}