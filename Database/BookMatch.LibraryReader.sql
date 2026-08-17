/* Ejecutar una vez sobre una base BookMatchDb existente. No elimina datos. */
USE BookMatchDb;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Biblioteca_Listar @UsuarioId int,@Filtro varchar(12)='all' AS
BEGIN
 SET NOCOUNT ON;
 SELECT l.LibroId,l.Codigo,l.Titulo,u.Nombre Autor,g.Nombre Genero,l.Idioma,l.Descripcion,l.Precio,l.Valoracion,l.PortadaUrl,l.Estado,l.Ventas,l.Descargas,b.Leido,b.FechaAdquisicion Fecha,
        CAST(CASE WHEN NULLIF(l.RutaPdf,N'') IS NULL THEN 0 ELSE 1 END AS bit) TienePdf
 FROM dbo.Biblioteca b
 JOIN dbo.Libros l ON l.LibroId=b.LibroId
 JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId
 JOIN dbo.Generos g ON g.GeneroId=l.GeneroId
 WHERE b.UsuarioId=@UsuarioId
   AND (@Filtro='all' OR @Filtro='read' AND b.Leido=1 OR @Filtro='pending' AND b.Leido=0)
 ORDER BY b.FechaAdquisicion DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Biblioteca_Acceso @UsuarioId int,@LibroId int,@MarcarLeido bit=0 AS
BEGIN
 SET NOCOUNT ON;
 IF @MarcarLeido=1
    UPDATE dbo.Biblioteca SET Leido=1 WHERE UsuarioId=@UsuarioId AND LibroId=@LibroId;

 SELECT l.LibroId,l.Titulo,u.Nombre Autor,l.RutaPdf
 FROM dbo.Biblioteca b
 JOIN dbo.Libros l ON l.LibroId=b.LibroId
 JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId
 WHERE b.UsuarioId=@UsuarioId AND b.LibroId=@LibroId;
END
GO

IF OBJECT_ID('dbo.usp_Biblioteca_Acceso','P') IS NULL
    THROW 50003, 'No se pudo instalar el lector de biblioteca.', 1;
PRINT 'Lector y descarga protegida instalados correctamente.';
