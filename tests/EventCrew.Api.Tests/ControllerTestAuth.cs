using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Tests;

internal static class ControllerTestAuth
{
    public static TController AsUser<TController>(TController controller, Guid userId, string role)
        where TController : ControllerBase
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", userId.ToString()), new Claim(ClaimTypes.Role, role)],
            authenticationType: "ControllerTest");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }
}
