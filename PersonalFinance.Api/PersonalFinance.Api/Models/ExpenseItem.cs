using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PersonalFinance.Api.Models
{
    /// <summary>
    /// Статья расходов — конкретный вид трат внутри категории.
    /// Например: «Обед в столовой», «Проезд в автобусе».
    /// Каждая статья принадлежит ровно одной категории.
    /// </summary>
    public class ExpenseItem
    {
        /// <summary>Уникальный идентификатор статьи расходов (автоинкремент).</summary>
        public int Id { get; set; }

        /// <summary>Наименование статьи расходов. Обязательное поле, не более 150 символов.</summary>
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Внешний ключ категории, к которой относится данная статья расходов.</summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Признак активности статьи расходов.
        /// При создании транзакции допускается выбор только активных статей.
        /// Деактивация статьи не затрагивает ранее созданные транзакции.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Навигационное свойство категории, к которой относится статья.
        /// Заполняется Entity Framework Core при использовании Include.
        /// </summary>
        [ValidateNever]
        public Category Category { get; set; } = null!;

        /// <summary>
        /// Коллекция транзакций, созданных по данной статье расходов.
        /// Используется для навигации в Entity Framework Core.
        /// </summary>
        [ValidateNever]
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}