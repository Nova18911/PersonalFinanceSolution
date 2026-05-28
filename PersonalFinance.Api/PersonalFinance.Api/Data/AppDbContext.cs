using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PersonalFinance.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<ExpenseItem> ExpenseItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ограничения и настройки связей

            modelBuilder.Entity<ExpenseItem>()
                .HasOne(e => e.Category)
                .WithMany(c => c.ExpenseItems)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Запрещаем удаление категории, если есть статьи

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.ExpenseItem)
                .WithMany(e => e.Transactions)
                .HasForeignKey(t => t.ExpenseItemId)
                .OnDelete(DeleteBehavior.Restrict); // Запрещаем удаление статьи, если есть транзакции

            // Индексы для быстрых запросов
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Date);
        }
    }
}