using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionShop.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }
        
        [Required]
        [StringLength(200)]
        public string CustomerEmail { get; set; }
        
        [StringLength(500)]
        public string? CustomerAddress { get; set; }
        
        [StringLength(100)]
        public string? CustomerCity { get; set; }
        
        [StringLength(50)]
        public string? CustomerProvince { get; set; }
        
        [StringLength(20)]
        public string? CustomerPostalCode { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
}