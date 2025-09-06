using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Get Cognito Access Token using Client Credentials Grant
    /// </summary>
    /// <returns>Access Token</returns>
    [HttpPost("token")]
    public async Task<IActionResult> GetCognitoToken()
    {
        var clientId = _configuration["Authentication:Cognito:ClientId"];
        var clientSecret = _configuration["Authentication:Cognito:ClientSecret"];
        var domain = _configuration["Authentication:Cognito:CognitoDomain"];
        var scope = _configuration["Authentication:Cognito:Scope"] ?? "resource.read";

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(domain))
        {
            return BadRequest("Cognito configuration is missing.");
        }

        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = $"{domain}/oauth2/token";

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", scope }
        };

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestBody));
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, content);
        }

        return Ok(JsonSerializer.Deserialize<Dictionary<string, object>>(content));
    }

    /// <summary>
    /// Front end log-in to get an authorization code. Then use the Authorization Code to get the access token
    /// </summary>
    [HttpPost("code-exchange")]
    public async Task<IActionResult> ExchangeCodeForToken([FromBody] CodeExchangeRequest request)
    {
        var clientId = _configuration["Authentication:Cognito:ClientId"];
        var clientSecret = _configuration["Authentication:Cognito:ClientSecret"];
        var domain = _configuration["Authentication:Cognito:CognitoDomain"];

        // read the front-end redirect URI from configuration
        var redirectUri = _configuration["Authentication:Cognito:RedirectUri"] ?? String.Empty;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(domain))
        {
            return BadRequest("Cognito configuration is missing.");
        }

        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = $"{domain}/oauth2/token";

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var body = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", request.Code },
            { "redirect_uri", redirectUri }
        };

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body));
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, content);
        }

        return Ok(JsonSerializer.Deserialize<Dictionary<string, object>>(content));
    }

    // This is a simple example for JWT token. In production, use a user store and hashed passwords.
    [HttpPost("login")]
    public IActionResult Login(string username, string password)
    {
        var adminUser = _configuration["Admin:Username"];
        var adminPass = _configuration["Admin:Password"];
        var jwtKey = _configuration["Jwt:Key"];

        // 判断角色
        string role;
        if (username == adminUser && password == adminPass)
        {
            role = "Admin";
        }
        else if (username == "user" && password == "user123")
        {
            role = "User";
        }
        else
        {
            return Unauthorized();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

}

public class CodeExchangeRequest
{
    public string Code { get; set; } = string.Empty;
}