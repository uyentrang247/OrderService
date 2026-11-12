using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class OrderItemInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    public string? Status { get; set; }

    public List<OrderItemInput>? Items { get; set; }
}

public class UpdateOrderRequest
{
    public string? CustomerName { get; set; }

    [EmailAddress]
    public string? CustomerEmail { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;
}

public class CreateOrderItemRequest
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderItemRequest
{
    public int? ProductId { get; set; }
    public int Quantity { get; set; }
}
