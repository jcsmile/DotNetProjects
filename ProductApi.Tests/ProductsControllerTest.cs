using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductApi.Controllers;
using ProductApi.Data;
using ProductApi.Models;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace ProductApi.Tests;

public class ProductsControllerTests
{
    private ProductDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // 每次测试用唯一 DB
            .Options;

        return new ProductDbContext(options);
    }

    private ProductsController GetController(ProductDbContext context, string role = "User")
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ProductsController>();
        var controller = new ProductsController(context, logger);
        controller.ControllerContext = ControllerContextHelper.WithUser("testUser", role);
        return controller;
    }

    [Fact]
    public async Task Create_Product_ShouldReturnCreatedProduct()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var product = new Product { Name = "Test Product", Price = 10.5M };
        var result = await controller.Create(product);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnProduct = Assert.IsType<Product>(createdResult.Value);

        Assert.Equal("Test Product", returnProduct.Name);
        Assert.Equal(10.5M, returnProduct.Price);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoProducts()
    {
        var context = GetDbContext();
        var controller = GetController(context);

        var result = await controller.GetAll();

        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(result.Value);
        Assert.Empty(products);
    }

    [Fact]
    public async Task Get_ShouldReturnProduct_WhenExists()
    {
        var context = GetDbContext();
        context.Products.Add(new Product { Id = 1, Name = "Existing", Price = 20M });
        context.SaveChanges();

        var controller = GetController(context);

        var result = await controller.Get(1);

        var product = Assert.IsType<Product>(result.Value);
        Assert.Equal("Existing", product.Name);
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenProductUpdated()
    {
        var context = GetDbContext();
        context.Products.Add(new Product { Id = 1, Name = "Old", Price = 30M });
        context.SaveChanges();

        var controller = GetController(context);
        var updatedProduct = new Product { Id = 1, Name = "New", Price = 40M };

        var result = await controller.Update(1, updatedProduct);

        Assert.IsType<NoContentResult>(result);

        var product = context.Products.Find(1);
        Assert.Equal("New", product!.Name);
        Assert.Equal(40M, product.Price);
    }

    [Fact]
    public async Task Admin_CanDelete_Product()
    {
        var context = GetDbContext();
        context.Products.Add(new Product { Id = 1, Name = "DeleteMe", Price = 50M });
        context.SaveChanges();

        var controller = GetController(context, "Admin");

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(context.Products.Find(1));
    }

    [Fact]
    public async Task User_CannotDelete_Product()
    {
        var context = GetDbContext();
        context.Products.Add(new Product { Id = 1, Name = "DeleteMe", Price = 50M });
        context.SaveChanges();

        var controller = GetController(context, "User");

        var result = await controller.Delete(1);

        // 模拟授权失败时 ASP.NET Core 会返回 ForbidResult
        Assert.IsType<ForbidResult>(result);
        Assert.NotNull(context.Products.Find(1)); // 产品还在
    }
}
