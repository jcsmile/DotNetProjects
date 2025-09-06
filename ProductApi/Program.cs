using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductApi.Data;
using ProductApi.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Use In-Memory Database for simplicity
builder.Services.AddDbContext<ProductDbContext>(opt =>
    opt.UseInMemoryDatabase("ProductDb"));

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Add this in your Program.cs 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ProductApi", Description = "The ultimate e-commerce product API", Version = "v1" });

    // Add OAuth2 configuration
    var cognito = builder.Configuration.GetSection("Authentication:Cognito");

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{cognito["CognitoDomain"]}/oauth2/authorize"),
                TokenUrl = new Uri($"{cognito["CognitoDomain"]}/oauth2/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect scope" },
                    { "email", "Access to your email" },
                    { "profile", "Access to your profile" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { "openid", "email", "profile" }
        }
    });
});

// Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];

// Ensure the JWT key is provided
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured.");
}

// Add Cognito Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var cognito = builder.Configuration.GetSection("Authentication:Cognito");
        options.Authority = cognito["Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cognito["Authority"],
            ValidateAudience = false,
            ValidAudience = cognito["ClientId"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");

        var cognito = builder.Configuration.GetSection("Authentication:Cognito");

        // 配置 Swagger UI 使用 Cognito 登录
        c.OAuthClientId(cognito["ClientId"]);
        c.OAuthClientSecret(cognito["ClientSecret"]);
        c.OAuthUsePkce(); // 推荐使用 PKCE
        c.OAuthScopes("openid", "email", "profile");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed some initial data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    ProductSeed.InitData(dbContext);
}
app.Run();
