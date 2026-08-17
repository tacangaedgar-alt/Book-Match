/* Ejecutar una vez sobre una base BookMatchDb existente. No elimina datos. */
USE BookMatchDb;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Libro_Catalogo @Busqueda nvarchar(180)=NULL,@Genero nvarchar(80)=NULL,@Idioma nvarchar(40)=NULL,@TipoPrecio varchar(10)='all',@ValoracionMinima decimal(3,2)=NULL,@UsuarioId int=NULL AS
BEGIN
 SET NOCOUNT ON;
 SELECT l.LibroId,l.Codigo,l.Titulo,u.Nombre Autor,g.Nombre Genero,l.Idioma,l.Descripcion,l.Precio,l.Valoracion,l.PortadaUrl,l.Estado,l.Ventas,l.Descargas,CAST(0 AS bit) Leido,CAST(l.FechaPublicacion AS datetime2) Fecha,
        CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Biblioteca b WHERE b.UsuarioId=@UsuarioId AND b.LibroId=l.LibroId) THEN 1 ELSE 0 END AS bit) EnBiblioteca,
        CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Carrito c WHERE c.UsuarioId=@UsuarioId AND c.LibroId=l.LibroId) THEN 1 ELSE 0 END AS bit) EnCarrito
 FROM dbo.Libros l JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId JOIN dbo.Generos g ON g.GeneroId=l.GeneroId
 WHERE l.Estado=N'Publicado'
   AND (@Busqueda IS NULL OR l.Titulo LIKE '%'+@Busqueda+'%' OR u.Nombre LIKE '%'+@Busqueda+'%')
   AND (@Genero IS NULL OR g.Nombre=@Genero) AND (@Idioma IS NULL OR l.Idioma=@Idioma)
   AND (@TipoPrecio='all' OR @TipoPrecio='free' AND l.Precio=0 OR @TipoPrecio='paid' AND l.Precio>0)
   AND (@ValoracionMinima IS NULL OR l.Valoracion>=@ValoracionMinima)
 ORDER BY l.FechaPublicacion DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Carrito_Agregar @UsuarioId int,@LibroId int AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.Libros WHERE LibroId=@LibroId AND Estado=N'Publicado') BEGIN SELECT 'not_found';RETURN;END
 IF EXISTS(SELECT 1 FROM dbo.Biblioteca WHERE UsuarioId=@UsuarioId AND LibroId=@LibroId) BEGIN SELECT 'owned';RETURN;END
 IF EXISTS(SELECT 1 FROM dbo.Libros WHERE LibroId=@LibroId AND Precio=0)
 BEGIN
   INSERT dbo.Biblioteca(UsuarioId,LibroId)VALUES(@UsuarioId,@LibroId);
   UPDATE dbo.Libros SET Descargas=Descargas+1 WHERE LibroId=@LibroId;
   SELECT 'library';RETURN;
 END
 IF EXISTS(SELECT 1 FROM dbo.Carrito WHERE UsuarioId=@UsuarioId AND LibroId=@LibroId) BEGIN SELECT 'already_cart';RETURN;END
 INSERT dbo.Carrito(UsuarioId,LibroId)VALUES(@UsuarioId,@LibroId);
 SELECT 'cart';
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Carrito_Contar @UsuarioId int AS
BEGIN SET NOCOUNT ON;SELECT COUNT(*) FROM dbo.Carrito WHERE UsuarioId=@UsuarioId;END
GO

IF OBJECT_ID('dbo.usp_Carrito_Contar','P') IS NULL THROW 50004,'No se pudo instalar el contador del carrito.',1;
PRINT 'Contador y estados del catálogo instalados correctamente.';
