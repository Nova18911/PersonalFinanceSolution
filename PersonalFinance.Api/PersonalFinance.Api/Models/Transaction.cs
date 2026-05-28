using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PersonalFinance.Api.Models
{
    /// <summary>
    /// Транзакция — факт совершения расходной операции.
    /// Содержит дату, сумму, необязательный комментарий и ссылку на статью расхода.
    /// Суммарные расходы за один день не могут превышать 1 000 000 рублей.
    /// </summary>
    public class Transaction
    {
        /// <summary>Уникальный идентификатор транзакции (автоинкремент).</summary>
        public int Id { get; set; }

        /// <summary>Дата совершения расходной операции. Обязательное поле.</summary>
        [Required]
        public DateOnly Date { get; set; }

        /// <summary>
        /// Сумма расходной операции в рублях. Обязательное поле.
        /// Допустимый диапазон: от 0,01 до 1 000 000 рублей включительно.
        /// </summary>
        [Required]
        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Произвольный комментарий к транзакции. Необязательное поле, не более 300 символов.
        /// </summary>
        [MaxLength(300)]
        public string? Comment { get; set; }

        /// <summary>Внешний ключ статьи расходов, к которой относится транзакция.</summary>
        public int ExpenseItemId { get; set; }

        /// <summary>
        /// Навигационное свойство статьи расходов.
        /// Заполняется Entity Framework Core при использовании Include.
        /// </summary>
        [ValidateNever]
        public ExpenseItem ExpenseItem { get; set; } = null!;
    }
}