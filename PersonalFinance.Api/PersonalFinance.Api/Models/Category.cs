using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PersonalFinance.Api.Models
{
    /// <summary>
    /// Категория расходов — группа однородных статей расходов.
    /// Например: «Питание», «Транспорт», «Развлечения».
    /// </summary>
    public class Category
    {
        /// <summary>Уникальный идентификатор категории (автоинкремент).</summary>
        public int Id { get; set; }

        /// <summary>Наименование категории. Обязательное поле, не более 100 символов.</summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Плановый месячный бюджет по категории в рублях. Не может быть отрицательным.</summary>
        [Range(0, double.MaxValue)]
        public decimal MonthlyBudget { get; set; }

        /// <summary>
        /// Признак активности категории.
        /// Неактивные категории недоступны для выбора при создании статей расходов.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Коллекция статей расходов, принадлежащих данной категории.
        /// Используется для навигации в Entity Framework Core.
        /// </summary>
        [ValidateNever]
        public ICollection<ExpenseItem> ExpenseItems { get; set; } = new List<ExpenseItem>();
    }
}