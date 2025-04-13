using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Payment
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } // e.g., CreditCard, PayPal, COD

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } // e.g., Pending, Completed, Failed

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string TransactionId { get; set; }
    }
}
