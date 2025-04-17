using System.ComponentModel.DataAnnotations;

namespace DACS_PetShop.Models
{
    // Model for Review
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
