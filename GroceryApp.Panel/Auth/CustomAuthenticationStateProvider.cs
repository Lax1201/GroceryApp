using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace GroceryApp.Panel.Auth;

/// <summary>
/// Provee el estado de autenticación para los componentes de Blazor Server
/// a partir del HttpContext actual (Cookie Authentication).
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new AuthenticationState(user));
        }

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(anonymousUser));
    }
}
