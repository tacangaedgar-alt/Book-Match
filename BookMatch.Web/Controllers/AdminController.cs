using System.Security.Claims;
using BookMatch.Web.Data;
using BookMatch.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookMatch.Web.Controllers;

[Authorize(Roles="Administrador")]
public sealed class AdminController(IBookMatchRepository repository) : Controller
{
    private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Dashboard()=>View(await repository.GetDashboardAsync(true,UserId));
    public async Task<IActionResult> Catalog(string? q,string? genre,string? language,string priceType="all",decimal? rating=null)=>View(new CatalogViewModel{Books=await repository.GetCatalogAsync(q,genre,language,priceType,rating),Query=q,Genre=genre,Language=language,PriceType=priceType,MinimumRating=rating});
    public async Task<IActionResult> Users(string? q,string tab="users")=>View(new UsersViewModel{Tab=tab,Users=await repository.GetUsersAsync(q)});
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> SetUserStatus(int id,bool active){await repository.SetUserStatusAsync(id,active);TempData["Success"]="Estado actualizado.";return RedirectToAction(nameof(Users));}
    public async Task<IActionResult> Queries(string tab="purchases")=>View(new QueryViewModel{Tab=tab,Books=await repository.GetCatalogAsync(null,null,null,"all",null),Purchases=await repository.GetPurchasesAsync()});
    public async Task<IActionResult> Reports()=>View(new PublicationViewModel{Books=await repository.GetCatalogAsync(null,null,null,"all",null),Purchases=await repository.GetPurchasesAsync()});
    public async Task<IActionResult> Security(string tab="roles")=>View("Users",new UsersViewModel{Tab=tab,Users=await repository.GetUsersAsync(null)});
}
