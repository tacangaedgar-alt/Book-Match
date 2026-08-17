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
    public async Task<IActionResult> Users(string? q,string tab="users")=>View(new UsersViewModel{Tab=tab,Query=q,Users=await repository.GetUsersAsync(q),Sessions=tab=="sessions"?await repository.GetActiveSessionsAsync():[]});
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserStatus(int id,string operation)
    {
        if(operation is not ("activate" or "deactivate")){TempData["Error"]="La operación solicitada no es válida.";return RedirectToAction(nameof(Users));}
        var active=operation=="activate";
        await repository.SetUserStatusAsync(id,active);
        TempData["Success"]=active?"Usuario activado correctamente.":"Usuario desactivado y sus sesiones fueron cerradas.";
        return RedirectToAction(nameof(Users));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserInput input)
    {
        if(!ModelState.IsValid){TempData["Error"]="Revisa el nombre, correo, rol y estado.";return RedirectToAction(nameof(Users));}
        try{await repository.UpdateUserAsync(UserId,input);TempData["Success"]="Usuario actualizado correctamente. Sus permisos se aplicarán en el próximo acceso.";}
        catch(Exception ex){TempData["Error"]=ex.Message.Contains("correo",StringComparison.OrdinalIgnoreCase)?"Ese correo ya pertenece a otro usuario.":"No se pudo actualizar el usuario.";}
        return RedirectToAction(nameof(Users));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSession(long id){await repository.CloseSessionAsync(id);TempData["Success"]="Sesión cerrada. El usuario será enviado al login en su próxima petición.";return RedirectToAction(nameof(Users),new{tab="sessions"});}
    public async Task<IActionResult> Queries(string tab="purchases")=>View(new QueryViewModel{Tab=tab,Books=await repository.GetCatalogAsync(null,null,null,"all",null),Purchases=await repository.GetPurchasesAsync()});
    public async Task<IActionResult> Reports()=>View(new PublicationViewModel{Books=await repository.GetCatalogAsync(null,null,null,"all",null),Purchases=await repository.GetPurchasesAsync()});
    public IActionResult Security(string tab="roles")=>RedirectToAction(nameof(Users),new{tab});
}
