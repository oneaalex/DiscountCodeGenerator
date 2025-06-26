using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiscountCodeService.Models
{
    [Index(nameof(Code), IsUnique = true)]
    public class DiscountCode
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime ExpirationDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsUsed { get; set; } = false;

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;

        public DateTime? DeletedAt { get; set; } = null;

        [NotMapped]
        public bool IsDeleted => DeletedAt.HasValue;

        public void MarkAsDeleted()
        {
            DeletedAt = DateTime.UtcNow;
            IsActive = false;
        }
    }
}