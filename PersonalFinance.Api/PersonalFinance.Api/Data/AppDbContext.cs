using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Data
{
    /// <summary>
    /// Контекст базы данных приложения учёта личных финансов.
    /// Предоставляет доступ к таблицам категорий, статей расходов и транзакций
    /// посредством Entity Framework Core.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Инициализирует контекст базы данных с переданными параметрами подключения.
        /// </summary>
        /// <param name="options">Параметры подключения, передаваемые через механизм внедрения зависимостей.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>Таблица категорий расходов.</summary>
        public DbSet<Category> Categories { get; set; }

        /// <summary>Таблица статей расходов.</summary>
        public DbSet<ExpenseItem> ExpenseItems { get; set; }

        /// <summary>Таблица транзакций (расходных операций).</summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// Настраивает схему базы данных: связи между сущностями,
        /// правила удаления и индексы для оптимизации запросов.
        /// </summary>
        /// <param name="modelBuilder">Построитель модели Entity Framework Core.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Связь «Статья расходов → Категория»: многие к одному.
            // Удаление категории запрещено при наличии связанных статей расходов.
            modelBuilder.Entity<ExpenseItem>()
                .HasOne(e => e.Category)
                .WithMany(c => c.ExpenseItems)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь «Транзакция → Статья расходов»: многие к одному.
            // Удаление статьи расходов запрещено при наличии связанных транзакций.
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.ExpenseItem)
                .WithMany(e => e.Transactions)
                .HasForeignKey(t => t.ExpenseItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Индекс по дате транзакции для ускорения выборок по конкретному дню или месяцу.
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Date);
        }
    }
}