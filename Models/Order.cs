using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models;

[Table("orders")]
public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> OrderItems { get; set; } = new();
}
