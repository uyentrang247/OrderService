using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly ProductApiClient _productClient;

    public OrdersController(OrderDbContext context, ProductApiClient productClient)
    {
        _context = context;
        _productClient = productClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .OrderBy(o => o.Id)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng có ID {id}" });

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var allowed = new[] { "pending", "completed", "cancelled" };
        var status = (request.Status ?? "pending").Trim().ToLower();
        if (!allowed.Contains(status))
            return BadRequest(new { message = "Trạng thái phải là pending, completed hoặc cancelled" });

        var token = Request.Headers.Authorization.ToString();
        var items = request.Items ?? new List<OrderItemInput>();
        var productMap = new Dictionary<int, ProductDto>();

        foreach (var group in items.GroupBy(i => i.ProductId))
        {
            var qty = group.Sum(x => x.Quantity);
            if (qty <= 0)
                return BadRequest(new { message = "Số lượng sản phẩm phải lớn hơn 0" });

            var product = await _productClient.GetProductAsync(group.Key, token);
            if (product == null)
                return NotFound(new { message = $"Không tìm thấy sản phẩm có ID {group.Key}" });

            if (status != "cancelled" && product.Quantity < qty)
                return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ tồn kho (còn {product.Quantity})" });

            productMap[group.Key] = product;
        }

        if (status != "cancelled")
        {
            foreach (var group in items.GroupBy(i => i.ProductId))
            {
                var product = productMap[group.Key];
                var ok = await _productClient.UpdateStockAsync(product, product.Quantity - group.Sum(x => x.Quantity), token);
                if (!ok)
                    return BadRequest(new { message = $"Không thể cập nhật tồn kho cho sản phẩm ID {group.Key}" });
            }
        }

        var order = new Order
        {
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems = items.Select(i =>
            {
                var p = productMap[i.ProductId];
                return new OrderItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Quantity = i.Quantity,
                    UnitPrice = p.Price,
                    TotalPrice = i.Quantity * p.Price
                };
            }).ToList()
        };
        order.TotalAmount = order.OrderItems.Sum(i => i.TotalPrice);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng có ID {id}" });

        var allowed = new[] { "pending", "completed", "cancelled" };
        var newStatus = request.Status.Trim().ToLower();
        if (!allowed.Contains(newStatus))
            return BadRequest(new { message = "Trạng thái phải là pending, completed hoặc cancelled" });

        var token = Request.Headers.Authorization.ToString();
        var oldStatus = order.Status.ToLower();

        if (oldStatus != "cancelled" && newStatus == "cancelled" && order.OrderItems.Count > 0)
        {
            await _productClient.RestoreStockAsync(order.OrderItems, token);
        }
        else if (oldStatus == "cancelled" && newStatus != "cancelled" && order.OrderItems.Count > 0)
        {
            foreach (var group in order.OrderItems.GroupBy(i => i.ProductId))
            {
                var product = await _productClient.GetProductAsync(group.Key, token);
                var qty = group.Sum(x => x.Quantity);
                if (product == null || product.Quantity < qty)
                    return BadRequest(new { message = $"Sản phẩm ID {group.Key} không đủ tồn kho để kích hoạt lại đơn" });
                await _productClient.UpdateStockAsync(product, product.Quantity - qty, token);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerName))
            order.CustomerName = request.CustomerName.Trim();

        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            order.CustomerEmail = request.CustomerEmail.Trim();

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(order);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng có ID {id}" });

        if (order.Status.ToLower() != "cancelled" && order.OrderItems.Count > 0)
            await _productClient.RestoreStockAsync(order.OrderItems, Request.Headers.Authorization.ToString());

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
