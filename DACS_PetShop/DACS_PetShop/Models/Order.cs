using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Order
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // e.g., Pending, Processing, Shipped, Delivered

        [Required]
        [StringLength(200)]
        public string ShippingAddress { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
        public Payment Payment { get; set; }
    }
}
