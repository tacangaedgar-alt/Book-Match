using BookMatch.Web.Models;

namespace BookMatch.Web.Data;

public interface IBookMatchRepository
{
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password);
    Task<DashboardViewModel> GetDashboardAsync(bool admin, int userId);
    Task SavePreferencesAsync(int userId, RecommendationInput input);
    Task<RecommendationInput?> GetPreferencesAsync(int userId);
    Task<List<BookItem>> GetRecommendationsAsync(int userId);
    Task<List<BookItem>> GetCatalogAsync(string? query, string? genre, string? language, string priceType, decimal? minimumRating, int? userId = null);
    Task<List<BookItem>> GetLibraryAsync(int userId, string filter);
    Task RateBookAsync(int userId, int bookId, int score);
    Task<LibraryBookAccess?> GetLibraryBookAccessAsync(int userId, int bookId, bool markAsRead);
    Task<List<BookItem>> GetPublicationsAsync(int userId);
    Task<LibraryBookAccess?> GetPublicationBookAccessAsync(int userId, int bookId);
    Task<int> PublishBookAsync(int userId, PublishBookInput input, string storedPdfPath, string coverUrl);
    Task<string> AddToCartAsync(int userId, int bookId);
    Task<int> GetCartCountAsync(int userId);
    Task<List<CartItem>> GetCartAsync(int userId);
    Task RemoveFromCartAsync(int userId, int bookId);
    Task CheckoutAsync(int userId, string paymentMethod, string paymentReference);
    Task<List<PurchaseRow>> GetPurchasesAsync();
    Task<List<UserRow>> GetUsersAsync(string? query);
    Task SetUserStatusAsync(int userId, bool active);
    Task UpdateUserAsync(int adminId, EditUserInput input);
    Task<long> StartSessionAsync(int userId, string? ipAddress);
    Task CloseSessionAsync(long sessionId);
    Task<List<SessionRow>> GetActiveSessionsAsync();
    Task<bool> IsSessionActiveAsync(long sessionId, int userId);
}
