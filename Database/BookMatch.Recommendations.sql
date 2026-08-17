USE BookMatchDb;
GO
SET NOCOUNT ON;
IF COL_LENGTH('dbo.Libros','NumeroPaginas') IS NULL ALTER TABLE dbo.Libros ADD NumeroPaginas int NULL;
IF COL_LENGTH('dbo.Libros','Formato') IS NULL ALTER TABLE dbo.Libros ADD Formato nvarchar(30) NOT NULL CONSTRAINT DF_Libros_Formato DEFAULT N'PDF';
GO

-- Las columnas deben existir antes de que SQL Server compile las instrucciones
-- que las utilizan en los lotes siguientes.
IF OBJECT_ID('dbo.PreferenciasUsuario') IS NULL CREATE TABLE dbo.PreferenciasUsuario(UsuarioId int NOT NULL PRIMARY KEY REFERENCES dbo.Usuarios(UsuarioId),Genero nvarchar(80) NOT NULL,Paginas nvarchar(40) NOT NULL,Idioma nvarchar(40) NOT NULL,Formato nvarchar(40) NOT NULL,Ritmo nvarchar(40) NOT NULL,Ambiente nvarchar(40) NOT NULL,Descubrimiento nvarchar(40) NOT NULL,Actualizado datetime2 NOT NULL DEFAULT sysdatetime());
UPDATE dbo.Libros SET NumeroPaginas=CASE (LibroId%5) WHEN 0 THEN 120 WHEN 1 THEN 220 WHEN 2 THEN 340 WHEN 3 THEN 510 ELSE 280 END,Formato=CASE (LibroId%3) WHEN 0 THEN N'Online' WHEN 1 THEN N'PDF' ELSE N'EPUB' END WHERE NumeroPaginas IS NULL;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Preferencia_Guardar @UsuarioId int,@Genero nvarchar(80),@Paginas nvarchar(40),@Idioma nvarchar(40),@Formato nvarchar(40),@Ritmo nvarchar(40),@Ambiente nvarchar(40),@Descubrimiento nvarchar(40) AS
BEGIN SET NOCOUNT ON;UPDATE dbo.PreferenciasUsuario SET Genero=@Genero,Paginas=@Paginas,Idioma=@Idioma,Formato=@Formato,Ritmo=@Ritmo,Ambiente=@Ambiente,Descubrimiento=@Descubrimiento,Actualizado=sysdatetime() WHERE UsuarioId=@UsuarioId;IF @@ROWCOUNT=0 INSERT dbo.PreferenciasUsuario(UsuarioId,Genero,Paginas,Idioma,Formato,Ritmo,Ambiente,Descubrimiento)VALUES(@UsuarioId,@Genero,@Paginas,@Idioma,@Formato,@Ritmo,@Ambiente,@Descubrimiento);END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Preferencia_Obtener @UsuarioId int AS
BEGIN SET NOCOUNT ON;SELECT Genero,Paginas,Idioma,Formato,Ritmo,Ambiente,Descubrimiento FROM dbo.PreferenciasUsuario WHERE UsuarioId=@UsuarioId;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Recomendacion_Obtener @UsuarioId int AS
BEGIN SET NOCOUNT ON;DECLARE @Genero nvarchar(80),@Paginas nvarchar(40),@Idioma nvarchar(40),@Formato nvarchar(40),@Ritmo nvarchar(40),@Ambiente nvarchar(40),@Descubrimiento nvarchar(40);SELECT @Genero=Genero,@Paginas=Paginas,@Idioma=Idioma,@Formato=Formato,@Ritmo=Ritmo,@Ambiente=Ambiente,@Descubrimiento=Descubrimiento FROM dbo.PreferenciasUsuario WHERE UsuarioId=@UsuarioId;
 SELECT l.LibroId,l.Codigo,l.Titulo,u.Nombre Autor,g.Nombre Genero,l.Idioma,l.Descripcion,l.Precio,l.Valoracion,l.PortadaUrl,l.Estado,l.Ventas,l.Descargas,CAST(0 AS bit) Leido,CAST(l.FechaPublicacion AS datetime2) Fecha,
 CAST((CASE WHEN g.Nombre=@Genero THEN 30 ELSE 0 END)+(CASE WHEN l.Idioma=@Idioma THEN 15 ELSE 0 END)+(CASE WHEN @Formato=N'Sin preferencia' OR l.Formato LIKE CASE WHEN @Formato LIKE N'%PDF%' THEN N'%PDF%' WHEN @Formato LIKE N'%línea%' THEN N'%Online%' WHEN @Formato LIKE N'%Epub%' THEN N'%EPUB%' ELSE N'%' END THEN 15 ELSE 0 END)+(CASE WHEN @Paginas=N'No tengo preferencia' OR @Paginas LIKE N'Menos%' AND l.NumeroPaginas<150 OR @Paginas=N'150–300 páginas' AND l.NumeroPaginas BETWEEN 150 AND 300 OR @Paginas=N'300–500 páginas' AND l.NumeroPaginas BETWEEN 301 AND 500 OR @Paginas LIKE N'Más%' AND l.NumeroPaginas>500 THEN 10 ELSE 0 END)+(CASE WHEN @Ritmo=N'Ágil y dinámico' AND g.Nombre IN(N'Thriller',N'Aventura',N'Ciencia Ficción') OR @Ritmo=N'Lento y reflexivo' AND g.Nombre IN(N'Filosofía',N'Poesía',N'Historia') OR @Ritmo=N'Equilibrado' THEN 10 ELSE 0 END)+(CASE WHEN @Ambiente=N'Misterioso' AND g.Nombre=N'Thriller' OR @Ambiente=N'Romántico' AND g.Nombre=N'Romance' OR @Ambiente=N'Épico' AND g.Nombre IN(N'Aventura',N'Ciencia Ficción') OR @Ambiente=N'Inspirador' AND g.Nombre IN(N'No Ficción',N'Poesía') OR @Ambiente=N'Realista' AND g.Nombre IN(N'Drama',N'Historia') THEN 10 ELSE 0 END)+(CASE WHEN @Descubrimiento=N'Tendencias' AND l.Ventas>=500 OR @Descubrimiento=N'Recomendaciones' AND l.Valoracion>=4.5 OR @Descubrimiento=N'Explorando géneros' AND g.Nombre<>@Genero OR @Descubrimiento=N'Autores favoritos' AND EXISTS(SELECT 1 FROM dbo.Biblioteca b JOIN dbo.Libros lb ON lb.LibroId=b.LibroId WHERE b.UsuarioId=@UsuarioId AND lb.AutorId=l.AutorId) THEN 10 ELSE 0 END) AS int) Afinidad
 FROM dbo.Libros l JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId JOIN dbo.Generos g ON g.GeneroId=l.GeneroId WHERE l.Estado=N'Publicado' ORDER BY Afinidad DESC,l.Valoracion DESC,l.Ventas DESC;
END
GO
IF OBJECT_ID('dbo.usp_Preferencia_Guardar','P') IS NULL OR OBJECT_ID('dbo.usp_Preferencia_Obtener','P') IS NULL OR OBJECT_ID('dbo.usp_Recomendacion_Obtener','P') IS NULL
    THROW 50002, 'No se pudieron instalar los procedimientos de recomendaciones.', 1;
PRINT 'Recomendaciones instaladas correctamente.';
