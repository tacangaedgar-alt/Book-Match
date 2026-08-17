using System.Data;
using BookMatch.Web.Models;
using Microsoft.Data.SqlClient;

namespace BookMatch.Web.Data;

public sealed class SqlBookMatchRepository(IConfiguration configuration) : IBookMatchRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("BookMatchDb")
        ?? throw new InvalidOperationException("Configura ConnectionStrings:BookMatchDb en appsettings.json.");

    private SqlConnection Connection() => new(_connectionString);
    private static SqlCommand Procedure(SqlConnection cn, string name)
        => new(name, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 30 };
    private static void Add(SqlCommand cmd, string name, object? value) => cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password)
    {
        await using var cn = Connection(); await cn.OpenAsync();
        await using var cmd = Procedure(cn, "dbo.usp_Auth_Login"); Add(cmd, "@Email", email); Add(cmd, "@Password", password);
        await using var rd = await cmd.ExecuteReaderAsync();
        return await rd.ReadAsync() ? new(rd.GetInt32("UsuarioId"), rd.GetString("Nombre"), rd.GetString("Correo"), rd.GetString("Rol"), rd.GetBoolean("EsAutor")) : null;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(bool admin, int userId)
    {
        await using var cn = Connection(); await cn.OpenAsync();
        await using var cmd = Procedure(cn, "dbo.usp_Dashboard_Obtener"); Add(cmd, "@UsuarioId", userId); Add(cmd, "@EsAdmin", admin);
        await using var rd = await cmd.ExecuteReaderAsync();
        var metrics = new List<Metric>();
        while (await rd.ReadAsync()) metrics.Add(new(rd.GetString("Etiqueta"), rd.GetString("Valor"), rd.GetString("Detalle"), rd.GetString("Icono"), rd.GetString("Tono")));
        var monthly = new List<MonthlyPoint>();
        if (await rd.NextResultAsync()) while (await rd.ReadAsync()) monthly.Add(new(rd.GetString("Mes"), rd.GetDecimal("Ventas"), rd.GetInt32("Descargas")));
        var genres = new List<GenrePoint>();
        if (await rd.NextResultAsync()) while (await rd.ReadAsync()) genres.Add(new(rd.GetString("Genero"), rd.GetInt32("Total")));
        return new() { Title = admin ? "Panel de Administración" : "Mi Dashboard", Metrics = metrics, Monthly = monthly, Genres = genres };
    }

    public async Task<List<BookItem>> GetCatalogAsync(string? query, string? genre, string? language, string priceType, decimal? minimumRating)
    {
        await using var cn = Connection(); await cn.OpenAsync(); await using var cmd = Procedure(cn, "dbo.usp_Libro_Catalogo");
        Add(cmd,"@Busqueda",query); Add(cmd,"@Genero",genre); Add(cmd,"@Idioma",language); Add(cmd,"@TipoPrecio",priceType); Add(cmd,"@ValoracionMinima",minimumRating);
        return await ReadBooksAsync(cmd);
    }

    public async Task<List<BookItem>> GetLibraryAsync(int userId, string filter)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Biblioteca_Listar"); Add(cmd,"@UsuarioId",userId); Add(cmd,"@Filtro",filter); return await ReadBooksAsync(cmd);
    }

    public async Task<List<BookItem>> GetPublicationsAsync(int userId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Publicacion_Listar"); Add(cmd,"@UsuarioId",userId); return await ReadBooksAsync(cmd);
    }

    public async Task<int> PublishBookAsync(int userId, PublishBookInput input, string? storedPdfPath)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Libro_Publicar");
        Add(cmd,"@UsuarioId",userId); Add(cmd,"@Titulo",input.Title); Add(cmd,"@Genero",input.Genre); Add(cmd,"@Idioma",input.Language); Add(cmd,"@Precio",input.Price); Add(cmd,"@Descripcion",input.Description); Add(cmd,"@Contenido",input.Content); Add(cmd,"@RutaPdf",storedPdfPath);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task AddToCartAsync(int userId,int bookId)=>await ExecuteAsync("dbo.usp_Carrito_Agregar",("@UsuarioId",userId),("@LibroId",bookId));
    public async Task RemoveFromCartAsync(int userId,int bookId)=>await ExecuteAsync("dbo.usp_Carrito_Eliminar",("@UsuarioId",userId),("@LibroId",bookId));
    public async Task CheckoutAsync(int userId)=>await ExecuteAsync("dbo.usp_Carrito_Comprar",("@UsuarioId",userId));

    public async Task<List<CartItem>> GetCartAsync(int userId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Carrito_Listar"); Add(cmd,"@UsuarioId",userId); await using var rd=await cmd.ExecuteReaderAsync(); var rows=new List<CartItem>();
        while(await rd.ReadAsync()) rows.Add(new(){BookId=rd.GetInt32("LibroId"),Title=rd.GetString("Titulo"),Author=rd.GetString("Autor"),Price=rd.GetDecimal("Precio")}); return rows;
    }

    public async Task<List<PurchaseRow>> GetPurchasesAsync()
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Consulta_Compras"); await using var rd=await cmd.ExecuteReaderAsync(); var rows=new List<PurchaseRow>();
        while(await rd.ReadAsync()) rows.Add(new(rd.GetString("Codigo"),rd.GetString("Usuario"),rd.GetString("Libro"),rd.GetString("Autor"),rd.GetDateTime("Fecha"),rd.GetDecimal("Monto"),rd.GetString("Estado"))); return rows;
    }

    public async Task<List<UserRow>> GetUsersAsync(string? query)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Usuario_Listar"); Add(cmd,"@Busqueda",query); await using var rd=await cmd.ExecuteReaderAsync(); var rows=new List<UserRow>();
        while(await rd.ReadAsync()) rows.Add(new(){Id=rd.GetInt32("UsuarioId"),Name=rd.GetString("Nombre"),Email=rd.GetString("Correo"),Role=rd.GetString("Rol"),Published=rd.GetInt32("Publicados"),Registered=rd.GetDateTime("Registrado"),Active=rd.GetBoolean("Activo")}); return rows;
    }

    public async Task SetUserStatusAsync(int userId,bool active)=>await ExecuteAsync("dbo.usp_Usuario_Estado",("@UsuarioId",userId),("@Activo",active));

    private async Task ExecuteAsync(string procedure, params (string Name,object Value)[] values)
    { await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,procedure); foreach(var value in values) Add(cmd,value.Name,value.Value); await cmd.ExecuteNonQueryAsync(); }

    private static async Task<List<BookItem>> ReadBooksAsync(SqlCommand cmd)
    {
        await using var rd=await cmd.ExecuteReaderAsync(); var rows=new List<BookItem>();
        while(await rd.ReadAsync()) rows.Add(new(){
            Id=rd.GetInt32("LibroId"), Code=rd.GetString("Codigo"), Title=rd.GetString("Titulo"), Author=rd.GetString("Autor"), Genre=rd.GetString("Genero"), Language=rd.GetString("Idioma"),
            Description=rd.GetString("Descripcion"), Price=rd.GetDecimal("Precio"), Rating=rd.GetDecimal("Valoracion"), CoverUrl=rd.GetString("PortadaUrl"), Status=rd.GetString("Estado"),
            Sales=rd.GetInt32("Ventas"), Downloads=rd.GetInt32("Descargas"), IsRead=rd.GetBoolean("Leido"), Date=rd.IsDBNull("Fecha")?null:rd.GetDateTime("Fecha")}); return rows;
    }
}
