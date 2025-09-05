namespace ProductApi.Models;

public class Product
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Department { get; set; } = string.Empty;
}
