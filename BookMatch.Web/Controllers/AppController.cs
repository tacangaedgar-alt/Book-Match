using System.Security.Claims;
using BookMatch.Web.Data;
using BookMatch.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BookMatch.Web.Controllers;

[Authorize(Roles = "Usuario")]
public sealed class AppController(IBookMatchRepository repository, IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Dashboard() => View(await repository.GetDashboardAsync(false, UserId));
    public async Task<IActionResult> Recommendations(bool edit=false)
    {
        if(edit)return View(new RecommendationViewModel());
        var preferences=await repository.GetPreferencesAsync(UserId);
        if(preferences is null)return View(new RecommendationViewModel());
        return View(new RecommendationViewModel{HasResults=true,Preferences=preferences,Books=await repository.GetRecommendationsAsync(UserId)});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Recommendations(RecommendationInput input)
    {
        if(!ModelState.IsValid){TempData["Error"]="Responde las siete preguntas para generar recomendaciones.";return View(new RecommendationViewModel{Preferences=input});}
        await repository.SavePreferencesAsync(UserId,input);
        var books=await repository.GetRecommendationsAsync(UserId);
        TempData["Success"]="Guardamos tus preferencias y encontramos lecturas para ti.";
        return View(new RecommendationViewModel{HasResults=true,Preferences=input,Books=books});
    }

    public async Task<IActionResult> Catalog(string? q, string? genre, string? language, string priceType="all", decimal? rating=null)
        => View(new CatalogViewModel { Books=await repository.GetCatalogAsync(q,genre,language,priceType,rating,UserId), Query=q,Genre=genre,Language=language,PriceType=priceType,MinimumRating=rating });

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int id)
    {
        var result=await repository.AddToCartAsync(UserId,id);
        if(result=="library")TempData["Success"]="Libro gratuito añadido a tu biblioteca.";
        else if(result=="cart")TempData["Success"]="Libro añadido al carrito.";
        else if(result=="owned")TempData["Error"]="Este libro ya está en tu biblioteca.";
        else if(result=="already_cart")TempData["Error"]="Este libro ya está en tu carrito.";
        else TempData["Error"]="El libro no está disponible.";
        return RedirectToAction(nameof(Catalog));
    }

    public async Task<IActionResult> Library(string filter="all") => View(new LibraryViewModel { Books=await repository.GetLibraryAsync(UserId,filter),Filter=filter });

    public async Task<IActionResult> ReadBook(int id)
    {
        var book=await repository.GetLibraryBookAccessAsync(UserId,id,false);
        if(book is null){TempData["Error"]="El libro no pertenece a tu biblioteca.";return RedirectToAction(nameof(Library));}
        if(!TryResolvePdf(book.PdfPath,out _)){TempData["Error"]="Este libro todavía no tiene un PDF disponible.";return RedirectToAction(nameof(Library));}
        await repository.GetLibraryBookAccessAsync(UserId,id,true);
        return View(new ReaderViewModel{BookId=book.Id,Title=book.Title,Author=book.Author});
    }

    public async Task<IActionResult> BookPdf(int id, bool download=false)
    {
        var book=await repository.GetLibraryBookAccessAsync(UserId,id,false);
        if(book is null)return NotFound();
        if(!TryResolvePdf(book.PdfPath,out var physicalPath))return NotFound();
        if(download)return PhysicalFile(physicalPath,"application/pdf",$"{SafeDownloadName(book.Title)}.pdf",enableRangeProcessing:true);
        return PhysicalFile(physicalPath,"application/pdf",enableRangeProcessing:true);
    }

    private bool TryResolvePdf(string? storedPath, out string physicalPath)
    {
        physicalPath="";
        if(string.IsNullOrWhiteSpace(storedPath))return false;
        var fileName=Path.GetFileName(storedPath);
        if(!string.Equals(Path.GetExtension(fileName),".pdf",StringComparison.OrdinalIgnoreCase))return false;
        physicalPath=Path.Combine(environment.WebRootPath,"uploads","books",fileName);
        return System.IO.File.Exists(physicalPath);
    }

    private static string SafeDownloadName(string title)
    {
        var invalid=Path.GetInvalidFileNameChars();
        var safe=new string(title.Select(c=>invalid.Contains(c)?'_':c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe)?"libro":safe;
    }
    public async Task<IActionResult> Publications(bool stats=false) => View(new PublicationViewModel { Books=await repository.GetPublicationsAsync(UserId),Statistics=stats });

    [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Publish(PublishBookInput input)
    {
        if (!ModelState.IsValid||input.Pdf is null||input.Cover is null) { TempData["Error"]="El PDF, la portada y los datos obligatorios son necesarios."; return RedirectToAction(nameof(Publications)); }
        var maxPdfMb=configuration.GetValue<int>("Uploads:MaxPdfSizeMb",50);
        if(input.Pdf.Length==0||input.Pdf.Length>maxPdfMb*1024L*1024L||!string.Equals(Path.GetExtension(input.Pdf.FileName),".pdf",StringComparison.OrdinalIgnoreCase)){TempData["Error"]=$"El PDF debe ser válido y pesar hasta {maxPdfMb} MB.";return RedirectToAction(nameof(Publications));}
        await using(var validationStream=input.Pdf.OpenReadStream())
        {
            var signature=new byte[5];if(await validationStream.ReadAsync(signature)!=5||!signature.SequenceEqual("%PDF-"u8.ToArray())){TempData["Error"]="El archivo seleccionado no es un PDF válido.";return RedirectToAction(nameof(Publications));}
        }
        var maxCoverMb=configuration.GetValue<int>("Uploads:MaxCoverSizeMb",5);
        var coverExtension=Path.GetExtension(input.Cover.FileName).ToLowerInvariant();
        if(input.Cover.Length==0||input.Cover.Length>maxCoverMb*1024L*1024L||!new[]{".jpg",".jpeg",".png",".webp"}.Contains(coverExtension)||!await IsValidCoverAsync(input.Cover,coverExtension)){TempData["Error"]=$"La portada debe ser JPG, PNG o WEBP y pesar hasta {maxCoverMb} MB.";return RedirectToAction(nameof(Publications));}
        var pdfFolder=Path.Combine(environment.WebRootPath,"uploads","books");var coverFolder=Path.Combine(environment.WebRootPath,"uploads","covers");Directory.CreateDirectory(pdfFolder);Directory.CreateDirectory(coverFolder);
        var pdfName=$"{Guid.NewGuid():N}.pdf";var coverName=$"{Guid.NewGuid():N}{coverExtension}";var pdfPhysical=Path.Combine(pdfFolder,pdfName);var coverPhysical=Path.Combine(coverFolder,coverName);
        await using(var pdfStream=System.IO.File.Create(pdfPhysical))await input.Pdf.CopyToAsync(pdfStream);
        await using(var coverStream=System.IO.File.Create(coverPhysical))await input.Cover.CopyToAsync(coverStream);
        try{await repository.PublishBookAsync(UserId,input,$"/uploads/books/{pdfName}",$"/uploads/covers/{coverName}");}
        catch{System.IO.File.Delete(pdfPhysical);System.IO.File.Delete(coverPhysical);throw;}
        TempData["Success"]="Tu libro y su portada se publicaron correctamente.";return RedirectToAction(nameof(Publications));
    }

    private static async Task<bool> IsValidCoverAsync(IFormFile file,string extension)
    {
        await using var stream=file.OpenReadStream();var header=new byte[12];var read=await stream.ReadAsync(header);
        return extension switch{".jpg" or ".jpeg"=>read>=3&&header[0]==0xFF&&header[1]==0xD8&&header[2]==0xFF,".png"=>read>=8&&header[..8].SequenceEqual(new byte[]{0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A}),".webp"=>read>=12&&header[..4].SequenceEqual("RIFF"u8.ToArray())&&header[8..12].SequenceEqual("WEBP"u8.ToArray()),_=>false};
    }

    public async Task<IActionResult> Cart() => View(new CartViewModel{Items=await repository.GetCartAsync(UserId),Checkout=new CheckoutInput{PayPalEmail=User.FindFirstValue(ClaimTypes.Email)??""}});
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> RemoveFromCart(int id){await repository.RemoveFromCartAsync(UserId,id);return RedirectToAction(nameof(Cart));}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CartViewModel model)
    {
        var items=await repository.GetCartAsync(UserId);
        var input=model.Checkout??new CheckoutInput();
        var userEmail=User.FindFirstValue(ClaimTypes.Email)??"";
        if(items.Count==0)ModelState.AddModelError("","Tu carrito está vacío.");
        if(input.PaymentMethod=="Card")
        {
            var digits=new string((input.CardNumber??"").Where(char.IsDigit).ToArray());
            if(string.IsNullOrWhiteSpace(input.Cardholder))ModelState.AddModelError("Checkout.Cardholder","Ingresa el nombre del titular.");
            if(!IsValidCardNumber(digits))ModelState.AddModelError("Checkout.CardNumber","Ingresa un número de tarjeta válido de 16 dígitos.");
            if(!IsValidExpiry(input.Expiry))ModelState.AddModelError("Checkout.Expiry","Ingresa una fecha vigente en formato MM/AA.");
            if((input.Cvv??"").Length is <3 or >4||!(input.Cvv??"").All(char.IsDigit))ModelState.AddModelError("Checkout.Cvv","El código de seguridad debe tener 3 o 4 dígitos.");
        }
        else if(input.PaymentMethod=="PayPal")
        {
            ModelState.Remove("Checkout.PayPalEmail");input.PayPalEmail=userEmail;
            if(string.IsNullOrWhiteSpace(userEmail))ModelState.AddModelError("Checkout.PayPalEmail","Tu cuenta no tiene un correo válido para PayPal.");
        }
        else ModelState.AddModelError("Checkout.PaymentMethod","Selecciona un método de pago válido.");
        if(!ModelState.IsValid)return View("Cart",new CartViewModel{Items=items,Checkout=input});
        var reference=input.PaymentMethod=="PayPal"?$"PayPal {userEmail}":$"Tarjeta **** {new string((input.CardNumber??"").Where(char.IsDigit).ToArray())[^4..]}";
        try{await repository.CheckoutAsync(UserId,input.PaymentMethod,reference);}
        catch(SqlException)
        {
            ModelState.AddModelError("","No se pudo registrar el pago. Ejecuta nuevamente Database/BookMatch.Full.sql para actualizar el procedimiento de compra.");
            return View("Cart",new CartViewModel{Items=items,Checkout=input});
        }
        TempData["Success"]=$"Compra simulada completada con {(input.PaymentMethod=="PayPal"?"PayPal":"tarjeta")}. Los libros ya están en tu biblioteca.";
        return RedirectToAction(nameof(Library));
    }

    private static bool IsValidCardNumber(string number)
    {
        // Es un checkout de demostración: se valida estructura, no autorización bancaria.
        return number.Length==16&&number.All(char.IsDigit);
    }

    private static bool IsValidExpiry(string? expiry)
    {
        var parts=(expiry??"").Split('/');if(parts.Length!=2||!int.TryParse(parts[0],out var month)||!int.TryParse(parts[1],out var year)||month is <1 or >12)return false;
        year+=2000;var now=DateTime.UtcNow;return year>now.Year||year==now.Year&&month>=now.Month;
    }
    public async Task<IActionResult> Queries(string tab="author") => View(new QueryViewModel{Tab=tab,Books=await repository.GetCatalogAsync(null,null,null,"all",null),Purchases=await repository.GetPurchasesAsync()});
    public async Task<IActionResult> Reports() => View(new PublicationViewModel { Books=await repository.GetCatalogAsync(null,null,null,"all",null),Purchases=await repository.GetPurchasesAsync() });
}
