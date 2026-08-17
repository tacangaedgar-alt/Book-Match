using System.ComponentModel.DataAnnotations;

namespace BookMatch.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public sealed record AuthenticatedUser(int Id, string Name, string Email, string Role, bool IsAuthor);
public sealed record Metric(string Label, string Value, string Detail, string Icon, string Tone);
public sealed record MonthlyPoint(string Month, decimal Sales, int Downloads);
public sealed record GenrePoint(string Genre, int Total);

public sealed class DashboardViewModel
{
    public List<Metric> Metrics { get; init; } = [];
    public List<MonthlyPoint> Monthly { get; init; } = [];
    public List<GenrePoint> Genres { get; init; } = [];
    public string Title { get; init; } = "Mi Dashboard";
}

public sealed class BookItem
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Title { get; init; } = "";
    public string Author { get; init; } = "";
    public string Genre { get; init; } = "";
    public string Language { get; init; } = "Español";
    public string Description { get; init; } = "";
    public decimal Price { get; init; }
    public decimal Rating { get; init; }
    public string CoverUrl { get; init; } = "";
    public string Status { get; init; } = "Publicado";
    public int Sales { get; init; }
    public int Downloads { get; init; }
    public bool IsRead { get; init; }
    public DateTime? Date { get; init; }
}

public sealed class CatalogViewModel
{
    public List<BookItem> Books { get; init; } = [];
    public string? Query { get; init; }
    public string? Genre { get; init; }
    public string? Language { get; init; }
    public string PriceType { get; init; } = "all";
    public decimal? MinimumRating { get; init; }
}

public sealed class LibraryViewModel
{
    public List<BookItem> Books { get; init; } = [];
    public int Read => Books.Count(x => x.IsRead);
    public int Pending => Books.Count(x => !x.IsRead);
    public string Filter { get; init; } = "all";
}

public sealed class PublicationViewModel
{
    public List<BookItem> Books { get; init; } = [];
    public bool Statistics { get; init; }
    public decimal Revenue => Books.Sum(x => x.Price * x.Sales);
    public int Downloads => Books.Sum(x => x.Downloads);
}

public sealed class PublishBookInput
{
    [Required, StringLength(180)] public string Title { get; set; } = "";
    [Required] public string Genre { get; set; } = "";
    [Required] public string Language { get; set; } = "Español";
    [Range(0, 9999)] public decimal Price { get; set; }
    [StringLength(2000)] public string Description { get; set; } = "";
    public string? Content { get; set; }
    public IFormFile? Pdf { get; set; }
}

public sealed class CartItem
{
    public int BookId { get; init; }
    public string Title { get; init; } = "";
    public string Author { get; init; } = "";
    public decimal Price { get; init; }
}

public sealed class QueryViewModel
{
    public string Tab { get; init; } = "author";
    public List<BookItem> Books { get; init; } = [];
    public List<PurchaseRow> Purchases { get; init; } = [];
}

public sealed record PurchaseRow(string Code, string User, string Book, string Author, DateTime Date, decimal Amount, string Status);

public sealed class UserRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public int Published { get; init; }
    public DateTime Registered { get; init; }
    public bool Active { get; init; }
}

public sealed class UsersViewModel
{
    public string Tab { get; init; } = "users";
    public List<UserRow> Users { get; init; } = [];
}
