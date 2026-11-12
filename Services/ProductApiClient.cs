using System.Net.Http.Json;
using OrderService.Models;

namespace OrderService.Services;

public class ProductApiClient
{
    private readonly HttpClient _http;

    public ProductApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProductDto?> GetProductAsync(int productId, string? token)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"/products/{productId}");
            if (!string.IsNullOrEmpty(token))
                req.Headers.TryAddWithoutValidation("Authorization", token);

            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
                return null;

            return await res.Content.ReadFromJsonAsync<ProductDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateStockAsync(ProductDto product, int newQuantity, string? token)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, $"/products/{product.Id}");
            if (!string.IsNullOrEmpty(token))
                req.Headers.TryAddWithoutValidation("Authorization", token);

            req.Content = JsonContent.Create(new
            {
                name = product.Name,
                description = product.Description,
                price = product.Price,
                quantity = newQuantity
            });

            var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task RestoreStockAsync(IEnumerable<OrderItem> items, string? token)
    {
        foreach (var group in items.GroupBy(i => i.ProductId))
        {
            var product = await GetProductAsync(group.Key, token);
            if (product != null)
                await UpdateStockAsync(product, product.Quantity + group.Sum(x => x.Quantity), token);
        }
    }
}
