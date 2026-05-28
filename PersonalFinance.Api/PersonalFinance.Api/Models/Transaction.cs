using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }

        [MaxLength(300)]
        public string? Comment { get; set; }

        public int ExpenseItemId { get; set; }

        // Навигационное свойство
        [ValidateNever]
        public ExpenseItem ExpenseItem { get; set; } = null!;
    }
}