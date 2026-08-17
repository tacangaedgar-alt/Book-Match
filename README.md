# BookMatch

Aplicación de biblioteca digital construida con ASP.NET Core MVC (.NET 8), Razor Views, autenticación por cookies, SQL Server y procedimientos almacenados.

## Instalación

1. Abre `Database/BookMatch.Full.sql` en SQL Server Management Studio y ejecútalo completo.
2. Ajusta `BookMatch.Web/appsettings.json` si tu instancia no usa `Server=localhost` y autenticación de Windows.
3. Desde esta carpeta ejecuta:

   ```powershell
   dotnet restore
   dotnet run --project .\BookMatch.Web\BookMatch.Web.csproj
   ```

4. Usa los accesos rápidos o estas cuentas:

   - Administrador: `admin@bookmatch.com`
   - Usuario/autor: `elena@example.com`
   - Contraseña: `password123`

Los PDF publicados se almacenan en `BookMatch.Web/wwwroot/uploads/books`; en producción conviene usar almacenamiento privado y servirlos mediante autorización.
