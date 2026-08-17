using System.Security.Claims;
using BookMatch.Web.Data;
using BookMatch.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookMatch.Web.Controllers;

[Authorize(Roles = "Usuario")]
public sealed class AppController(IBookMatchRepository repository, IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Dashboard() => View(await repository.GetDashboardAsync(false, UserId));
    public IActionResult Recommendations() => View();

    public async Task<IActionResult> Catalog(string? q, string? genre, string? language, string priceType="all", decimal? rating=null)
        => View(new CatalogViewModel { Books=await repository.GetCatalogAsync(q,genre,language,priceType,rating), Query=q,Genre=genre,Language=language,PriceType=priceType,MinimumRating=rating });

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int id) { await repository.AddToCartAsync(UserId,id); TempData["Success"]="Libro añadido al carrito."; return RedirectToAction(nameof(Catalog)); }

    public async Task<IActionResult> Library(string filter="all") => View(new LibraryViewModel { Books=await repository.GetLibraryAsync(UserId,filter),Filter=filter });
    public async Task<IActionResult> Publications(bool stats=false) => View(new PublicationViewModel { Books=await repository.GetPublicationsAsync(UserId),Statistics=stats });

    [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(55_000_000)]
    public async Task<IActionResult> Publish(PublishBookInput input)
    {
        if (!ModelState.IsValid) { TempData["Error"]="Revisa los campos obligatorios."; return RedirectToAction(nameof(Publications)); }
        string? relativePath=null;
        if(input.Pdf is { Length: > 0 })
        {
            var maxMb=configuration.GetValue<int>("Uploads:MaxPdfSizeMb",50);
            if(input.Pdf.Length>maxMb*1024L*1024L || !string.Equals(Path.GetExtension(input.Pdf.FileName),".pdf",StringComparison.OrdinalIgnoreCase)) { TempData["Error"]=$"Solo se aceptan PDF de hasta {maxMb} MB."; return RedirectToAction(nameof(Publications)); }
            await using (var validationStream=input.Pdf.OpenReadStream())
            {
                var signature=new byte[5];
                if(await validationStream.ReadAsync(signature)!=5 || !signature.SequenceEqual("%PDF-"u8.ToArray())) { TempData["Error"]="El archivo seleccionado no es un PDF válido."; return RedirectToAction(nameof(Publications)); }
            }
            var folder=Path.Combine(environment.WebRootPath,"uploads","books"); Directory.CreateDirectory(folder);
            var fileName=$"{Guid.NewGuid():N}.pdf"; await using var stream=System.IO.File.Create(Path.Combine(folder,fileName)); await input.Pdf.CopyToAsync(stream); relativePath=$"/uploads/books/{fileName}";
        }
        await repository.PublishBookAsync(UserId,input,relativePath); TempData["Success"]="Tu libro fue enviado a publicación."; return RedirectToAction(nameof(Publications));
    }

    public async Task<IActionResult> Cart() => View(await repository.GetCartAsync(UserId));
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> RemoveFromCart(int id){await repository.RemoveFromCartAsync(UserId,id);return RedirectToAction(nameof(Cart));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Checkout(){await repository.CheckoutAsync(UserId);TempData["Success"]="Compra completada. Los libros ya están en tu biblioteca.";return RedirectToAction(nameof(Library));}
    public async Task<IActionResult> Queries(string tab="author") => View(new QueryViewModel{Tab=tab,Books=await repository.GetCatalogAsync(null,null,null,"all",null),Purchases=await repository.GetPurchasesAsync()});
    public async Task<IActionResult> Reports() => View(new PublicationViewModel { Books=await repository.GetCatalogAsync(null,null,null,"all",null) });
}
