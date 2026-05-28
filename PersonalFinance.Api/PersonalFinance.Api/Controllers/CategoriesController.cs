using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Controllers
{
    /// <summary>
    /// Контроллер для управления категориями расходов.
    /// Предоставляет REST API для получения, создания, обновления и удаления категорий.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Инициализирует контроллер с контекстом базы данных.
        /// </summary>
        /// <param name="context">Контекст базы данных, предоставляемый через внедрение зависимостей.</param>
        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает список всех категорий расходов вместе со связанными статьями расходов.
        /// </summary>
        /// <returns>Коллекция категорий расходов.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories
                                 .Include(c => c.ExpenseItems)
                                 .ToListAsync();
        }

        /// <summary>
        /// Возвращает категорию расходов по указанному идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <returns>Категория расходов или 404, если не найдена.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                                         .Include(c => c.ExpenseItems)
                                         .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return NotFound();

            return category;
        }

        /// <summary>
        /// Создаёт новую категорию расходов.
        /// </summary>
        /// <param name="category">Данные создаваемой категории.</param>
        /// <returns>Созданная категория с присвоенным идентификатором.</returns>
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        /// <summary>
        /// Обновляет существующую категорию расходов.
        /// </summary>
        /// <param name="id">Идентификатор обновляемой категории.</param>
        /// <param name="category">Новые данные категории. Поле Id должно совпадать с параметром id.</param>
        /// <returns>204 NoContent при успехе, 400 или 404 при ошибке.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, Category category)
        {
            if (id != category.Id)
                return BadRequest();

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Удаляет категорию расходов по указанному идентификатору.
        /// Удаление запрещено, если по статьям категории существуют транзакции.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой категории.</param>
        /// <returns>204 NoContent при успехе, 400 если удаление запрещено, 404 если не найдена.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                                         .Include(c => c.ExpenseItems)
                                             .ThenInclude(e => e.Transactions)
                                         .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            // Запрещаем удаление категории, если хотя бы по одной из её статей есть транзакции
            if (category.ExpenseItems.Any(e => e.Transactions.Any()))
                return BadRequest("Нельзя удалить категорию, по статьям которой есть транзакции.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Проверяет существование категории с указанным идентификатором.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <returns>True, если категория существует; иначе false.</returns>
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}