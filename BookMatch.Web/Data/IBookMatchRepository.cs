using BookMatch.Web.Models;

namespace BookMatch.Web.Data;

public interface IBookMatchRepository
{
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password);
    Task<DashboardViewModel> GetDashboardAsync(bool admin, int userId);
    Task SavePreferencesAsync(int userId, RecommendationInput input);
    Task<RecommendationInput?> GetPreferencesAsync(int userId);
    Task<List<BookItem>> GetRecommendationsAsync(int userId);
    Task<List<BookItem>> GetCatalogAsync(string? query, string? genre, string? language, string priceType, decimal? minimumRating);
    Task<List<BookItem>> GetLibraryAsync(int userId, string filter);
    Task<LibraryBookAccess?> GetLibraryBookAccessAsync(int userId, int bookId, bool markAsRead);
    Task<List<BookItem>> GetPublicationsAsync(int userId);
    Task<int> PublishBookAsync(int userId, PublishBookInput input, string? storedPdfPath);
    Task AddToCartAsync(int userId, int bookId);
    Task<List<CartItem>> GetCartAsync(int userId);
    Task RemoveFromCartAsync(int userId, int bookId);
    Task CheckoutAsync(int userId);
    Task<List<PurchaseRow>> GetPurchasesAsync();
    Task<List<UserRow>> GetUsersAsync(string? query);
    Task SetUserStatusAsync(int userId, bool active);
}
