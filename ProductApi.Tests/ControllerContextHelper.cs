using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProductApi.Tests;

public static class ControllerContextHelper
{
    public static ControllerContext WithUser(string username, string role)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        }, "mock"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
