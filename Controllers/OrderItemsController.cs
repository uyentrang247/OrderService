using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("order_items")]
[Authorize]
public class OrderItemsController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly ProductApiClient _productClient;

    public OrderItemsController(OrderDbContext context, ProductApiClient productClient)
    {
        _context = context;
        _productClient = productClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderItem>>> GetOrderItems()
    {
        var items = await _context.OrderItems
            .AsNoTracking()
            .OrderBy(i => i.Id)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderItem>> GetOrderItem(int id)
    {
        var item = await _context.OrderItems.FindAsync(id);
        if (item == null)
            return NotFound(new { message = $"Không tìm thấy chi tiết đơn hàng có ID {id}" });

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<OrderItem>> CreateOrderItem([FromBody] CreateOrderItemRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Số lượng phải lớn hơn 0" });

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng có ID {request.OrderId}" });

        var token = Request.Headers.Authorization.ToString();
        var product = await _productClient.GetProductAsync(request.ProductId, token);
        if (product == null)
            return NotFound(new { message = $"Không tìm thấy sản phẩm có ID {request.ProductId}" });

        if (order.Status.ToLower() != "cancelled")
        {
            if (product.Quantity < request.Quantity)
                return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ tồn kho (còn {product.Quantity})" });

            await _productClient.UpdateStockAsync(product, product.Quantity - request.Quantity, token);
        }

        var item = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            TotalPrice = request.Quantity * product.Price
        };

        order.OrderItems.Add(item);
        order.TotalAmount = order.OrderItems.Sum(i => i.TotalPrice);
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderItem), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOrderItem(int id, [FromBody] UpdateOrderItemRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Số lượng phải lớn hơn 0" });

        var item = await _context.OrderItems.FindAsync(id);
        if (item == null)
            return NotFound(new { message = $"Không tìm thấy chi tiết đơn hàng có ID {id}" });

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == item.OrderId);

        if (order == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng có ID {item.OrderId}" });

        var token = Request.Headers.Authorization.ToString();
        var productId = request.ProductId ?? item.ProductId;
        var product = await _productClient.GetProductAsync(productId, token);
        if (product == null)
            return NotFound(new { message = $"Không tìm thấy sản phẩm có ID {productId}" });

        if (order.Status.ToLower() != "cancelled")
        {
            if (productId == item.ProductId)
            {
                var diff = request.Quantity - item.Quantity;
                if (diff > 0 && product.Quantity < diff)
                    return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ tồn kho (còn {product.Quantity})" });

                if (diff != 0)
                    await _productClient.UpdateStockAsync(product, product.Quantity - diff, token);
            }
            else
            {
                if (product.Quantity < request.Quantity)
                    return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ tồn kho (còn {product.Quantity})" });

                await _productClient.UpdateStockAsync(product, product.Quantity - request.Quantity, token);
                await _productClient.RestoreStockAsync(new[] { item }, token);
            }
        }

        item.ProductId = product.Id;
        item.ProductName = product.Name;
        item.Quantity = request.Quantity;
        item.UnitPrice = product.Price;
        item.TotalPrice = request.Quantity * product.Price;

        order.TotalAmount = order.OrderItems.Sum(i => i.TotalPrice);
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var item = await _context.OrderItems.FindAsync(id);
        if (item == null)
            return NotFound(new { message = $"Không tìm thấy chi tiết đơn hàng có ID {id}" });

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == item.OrderId);

        if (order != null && order.Status.ToLower() != "cancelled")
            await _productClient.RestoreStockAsync(new[] { item }, Request.Headers.Authorization.ToString());

        _context.OrderItems.Remove(item);

        if (order != null)
        {
            order.TotalAmount = order.OrderItems.Where(i => i.Id != item.Id).Sum(i => i.TotalPrice);
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
