using System.Security.Claims;
using BookMatch.Web.Data;
using BookMatch.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BookMatch.Web.Controllers;

public sealed class AccountController(IBookMatchRepository repository) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", User.IsInRole("Administrador") ? "Admin" : "App");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var user = await repository.AuthenticateAsync(model.Email.Trim(), model.Password);
            if (user is null) { ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos."); return View(model); }
            await SignInAsync(user);
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
            return RedirectToAction("Dashboard", user.Role == "Administrador" ? "Admin" : "App");
        }
        catch (SqlException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo conectar con BookMatchDb. Ejecuta primero Database/BookMatch.Full.sql y revisa appsettings.json.");
            return View(model);
        }
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Demo(string role)
    {
        var email = role == "admin" ? "admin@bookmatch.com" : "elena@example.com";
        var user = await repository.AuthenticateAsync(email, "password123");
        if (user is null) return RedirectToAction(nameof(Login));
        await SignInAsync(user);
        return RedirectToAction("Dashboard", role == "admin" ? "Admin" : "App");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(AuthenticatedUser user)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()), new Claim(ClaimTypes.Name,user.Name), new Claim(ClaimTypes.Email,user.Email), new Claim(ClaimTypes.Role,user.Role), new Claim("IsAuthor",user.IsAuthor.ToString()) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
    }
}
