using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Api.Models
{
    public class ExpenseItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        [ValidateNever]
        public Category Category { get; set; } = null!;
        [ValidateNever]
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}