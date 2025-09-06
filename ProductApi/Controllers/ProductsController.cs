using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductApi.Data;
using ProductApi.Models;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // [HttpGet]
    // public async Task<ActionResult<IEnumerable<Product>>> GetAll() =>
    //     await _context.Products.ToListAsync();

    [HttpGet]
    public ActionResult<IQueryable<Product>> GetProducts([FromQuery] ProductQueryParams request)
    {
        var result = _context.Products as IQueryable<Product>;

        if (!string.IsNullOrWhiteSpace(request.Department))
            result = result.Where(p => String.Equals(p.Department, request.Department, StringComparison.CurrentCultureIgnoreCase)); 

        Response.Headers["x-total-count"] = result.Count().ToString();

        return Ok(result
            .OrderBy(p => p.Id)
            .Skip(request.Offset)
            .Take(request.Limit));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> Get(string id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? NotFound() : product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        _logger.LogInformation("Creating product: {ProductName}", product.Name);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Product product)
    {
        if (!string.Equals(id, product.Id, StringComparison.CurrentCultureIgnoreCase)) return BadRequest();
        _logger.LogInformation("Updating product: {ProductId}", product.Id);
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize] // Only Admins can delete
    public async Task<IActionResult> Delete(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        _logger.LogInformation("Deleting product: {ProductId}", id);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
