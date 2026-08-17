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

    public async Task<List<BookItem>> GetCatalogAsync(string? query, string? genre, string? language, string priceType, decimal? minimumRating, int? userId = null)
    {
        await using var cn = Connection(); await cn.OpenAsync(); await using var cmd = Procedure(cn, "dbo.usp_Libro_Catalogo");
        Add(cmd,"@Busqueda",query); Add(cmd,"@Genero",genre); Add(cmd,"@Idioma",language); Add(cmd,"@TipoPrecio",priceType); Add(cmd,"@ValoracionMinima",minimumRating); Add(cmd,"@UsuarioId",userId);
        return await ReadBooksAsync(cmd);
    }

    public async Task SavePreferencesAsync(int userId, RecommendationInput input)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Preferencia_Guardar");
        Add(cmd,"@UsuarioId",userId); Add(cmd,"@Genero",input.Genre); Add(cmd,"@Paginas",input.PagePreference); Add(cmd,"@Idioma",input.Language); Add(cmd,"@Formato",input.Format); Add(cmd,"@Ritmo",input.Pace); Add(cmd,"@Ambiente",input.Mood); Add(cmd,"@Descubrimiento",input.Discovery); await cmd.ExecuteNonQueryAsync();
    }

    public async Task<RecommendationInput?> GetPreferencesAsync(int userId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Preferencia_Obtener"); Add(cmd,"@UsuarioId",userId);
        await using var rd=await cmd.ExecuteReaderAsync();
        return await rd.ReadAsync()?new RecommendationInput{Genre=rd.GetString("Genero"),PagePreference=rd.GetString("Paginas"),Language=rd.GetString("Idioma"),Format=rd.GetString("Formato"),Pace=rd.GetString("Ritmo"),Mood=rd.GetString("Ambiente"),Discovery=rd.GetString("Descubrimiento")}:null;
    }

    public async Task<List<BookItem>> GetRecommendationsAsync(int userId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Recomendacion_Obtener"); Add(cmd,"@UsuarioId",userId); return await ReadBooksAsync(cmd);
    }

    public async Task<List<BookItem>> GetLibraryAsync(int userId, string filter)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Biblioteca_Listar"); Add(cmd,"@UsuarioId",userId); Add(cmd,"@Filtro",filter); return await ReadBooksAsync(cmd);
    }

    public async Task RateBookAsync(int userId, int bookId, int score)
        => await ExecuteAsync("dbo.usp_Valoracion_Guardar",("@UsuarioId",userId),("@LibroId",bookId),("@Puntuacion",score));

    public async Task<LibraryBookAccess?> GetLibraryBookAccessAsync(int userId, int bookId, bool markAsRead)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Biblioteca_Acceso");
        Add(cmd,"@UsuarioId",userId); Add(cmd,"@LibroId",bookId); Add(cmd,"@MarcarLeido",markAsRead);
        await using var rd=await cmd.ExecuteReaderAsync();
        return await rd.ReadAsync() ? new(rd.GetInt32("LibroId"),rd.GetString("Titulo"),rd.GetString("Autor"),rd.IsDBNull("RutaPdf")?null:rd.GetString("RutaPdf")) : null;
    }

    public async Task<List<BookItem>> GetPublicationsAsync(int userId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Publicacion_Listar"); Add(cmd,"@UsuarioId",userId); return await ReadBooksAsync(cmd);
    }

    public async Task<LibraryBookAccess?> GetPublicationBookAccessAsync(int userId, int bookId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Publicacion_Acceso");
        Add(cmd,"@UsuarioId",userId); Add(cmd,"@LibroId",bookId); await using var rd=await cmd.ExecuteReaderAsync();
        return await rd.ReadAsync()?new(rd.GetInt32("LibroId"),rd.GetString("Titulo"),rd.GetString("Autor"),rd.IsDBNull("RutaPdf")?null:rd.GetString("RutaPdf")):null;
    }

    public async Task<int> PublishBookAsync(int userId, PublishBookInput input, string storedPdfPath, string coverUrl)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Libro_Publicar");
        Add(cmd,"@UsuarioId",userId); Add(cmd,"@Titulo",input.Title); Add(cmd,"@Genero",input.Genre); Add(cmd,"@Idioma",input.Language); Add(cmd,"@Precio",input.Price); Add(cmd,"@Descripcion",input.Description); Add(cmd,"@Contenido",input.Content); Add(cmd,"@RutaPdf",storedPdfPath); Add(cmd,"@PortadaUrl",coverUrl);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<string> AddToCartAsync(int userId,int bookId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Carrito_Agregar"); Add(cmd,"@UsuarioId",userId); Add(cmd,"@LibroId",bookId);
        return Convert.ToString(await cmd.ExecuteScalarAsync())??"not_found";
    }

    public async Task<int> GetCartCountAsync(int userId)
    {
        await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,"dbo.usp_Carrito_Contar"); Add(cmd,"@UsuarioId",userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
    public async Task RemoveFromCartAsync(int userId,int bookId)=>await ExecuteAsync("dbo.usp_Carrito_Eliminar",("@UsuarioId",userId),("@LibroId",bookId));
    public async Task CheckoutAsync(int userId,string paymentMethod,string paymentReference)=>await ExecuteAsync("dbo.usp_Carrito_Comprar",("@UsuarioId",userId),("@MetodoPago",paymentMethod),("@ReferenciaPago",paymentReference));

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
        while(await rd.ReadAsync()){var role=rd.GetString("Rol");rows.Add(new(){Id=rd.GetInt32("UsuarioId"),Name=rd.GetString("Nombre"),Email=rd.GetString("Correo"),Role=role=="Usuario"?"Lector / Escritor":role,Published=rd.GetInt32("Publicados"),Registered=rd.GetDateTime("Registrado"),Active=rd.GetBoolean("Activo")});} return rows;
    }

    public async Task SetUserStatusAsync(int userId,bool active)=>await ExecuteAsync("dbo.usp_Usuario_Estado",("@UsuarioId",userId),("@Activo",active));

    public async Task UpdateUserAsync(int adminId,EditUserInput input)=>await ExecuteAsync("dbo.usp_Usuario_Actualizar",("@AdministradorId",adminId),("@UsuarioId",input.Id),("@Nombre",input.Name),("@Correo",input.Email),("@Rol",input.Role),("@Activo",input.Active));
    public async Task CreateUserAsync(CreateUserInput input)=>await ExecuteAsync("dbo.usp_Usuario_Crear",("@Nombre",input.Name),("@Correo",input.Email),("@Password",input.Password),("@Rol",input.Role),("@Activo",input.Active));

    public async Task<long> StartSessionAsync(int userId,string? ipAddress)
    { await using var cn=Connection();await cn.OpenAsync();await using var cmd=Procedure(cn,"dbo.usp_Sesion_Iniciar");Add(cmd,"@UsuarioId",userId);Add(cmd,"@DireccionIp",ipAddress);return Convert.ToInt64(await cmd.ExecuteScalarAsync()); }

    public async Task CloseSessionAsync(long sessionId)=>await ExecuteAsync("dbo.usp_Sesion_Cerrar",("@SesionId",sessionId));

    public async Task<List<SessionRow>> GetActiveSessionsAsync()
    { await using var cn=Connection();await cn.OpenAsync();await using var cmd=Procedure(cn,"dbo.usp_Sesion_ListarActivas");await using var rd=await cmd.ExecuteReaderAsync();var rows=new List<SessionRow>();while(await rd.ReadAsync())rows.Add(new(rd.GetInt64("SesionId"),rd.GetInt32("UsuarioId"),rd.GetString("Usuario"),rd.GetString("Correo"),rd.GetString("Rol"),rd.GetDateTime("Inicio"),rd.IsDBNull("DireccionIp")?null:rd.GetString("DireccionIp")));return rows; }

    public async Task<bool> IsSessionActiveAsync(long sessionId,int userId)
    { await using var cn=Connection();await cn.OpenAsync();await using var cmd=Procedure(cn,"dbo.usp_Sesion_Validar");Add(cmd,"@SesionId",sessionId);Add(cmd,"@UsuarioId",userId);return Convert.ToBoolean(await cmd.ExecuteScalarAsync()); }

    private async Task ExecuteAsync(string procedure, params (string Name,object Value)[] values)
    { await using var cn=Connection(); await cn.OpenAsync(); await using var cmd=Procedure(cn,procedure); foreach(var value in values) Add(cmd,value.Name,value.Value); await cmd.ExecuteNonQueryAsync(); }

    private static async Task<List<BookItem>> ReadBooksAsync(SqlCommand cmd)
    {
        await using var rd=await cmd.ExecuteReaderAsync(); var rows=new List<BookItem>();
        while(await rd.ReadAsync()) rows.Add(new(){
            Id=rd.GetInt32("LibroId"), Code=rd.GetString("Codigo"), Title=rd.GetString("Titulo"), Author=rd.GetString("Autor"), Genre=rd.GetString("Genero"), Language=rd.GetString("Idioma"),
            Description=rd.GetString("Descripcion"), Price=rd.GetDecimal("Precio"), Rating=rd.GetDecimal("Valoracion"), CoverUrl=rd.GetString("PortadaUrl"), Status=rd.GetString("Estado"),
            Sales=rd.GetInt32("Ventas"), Downloads=rd.GetInt32("Descargas"), IsRead=rd.GetBoolean("Leido"), HasPdf=HasColumn(rd,"TienePdf")&&rd.GetBoolean("TienePdf"), IsInLibrary=HasColumn(rd,"EnBiblioteca")&&rd.GetBoolean("EnBiblioteca"), IsInCart=HasColumn(rd,"EnCarrito")&&rd.GetBoolean("EnCarrito"), IsOwnPublication=HasColumn(rd,"EsPropio")&&rd.GetBoolean("EsPropio"), RatingCount=HasColumn(rd,"CantidadValoraciones")?rd.GetInt32("CantidadValoraciones"):0, UserRating=HasColumn(rd,"ValoracionUsuario")?rd.GetInt32("ValoracionUsuario"):0, Date=rd.IsDBNull("Fecha")?null:rd.GetDateTime("Fecha"), Affinity=HasColumn(rd,"Afinidad")?rd.GetInt32("Afinidad"):0}); return rows;
    }

    private static bool HasColumn(SqlDataReader reader,string name){for(var i=0;i<reader.FieldCount;i++)if(string.Equals(reader.GetName(i),name,StringComparison.OrdinalIgnoreCase))return true;return false;}
}
