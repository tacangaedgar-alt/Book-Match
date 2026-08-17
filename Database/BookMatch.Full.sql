/* BookMatch - instalación completa y repetible para SQL Server 2019+ */
SET NOCOUNT ON;
IF DB_ID(N'BookMatchDb') IS NULL CREATE DATABASE BookMatchDb;
GO
USE BookMatchDb;
GO

IF OBJECT_ID('dbo.Roles') IS NULL CREATE TABLE dbo.Roles(RolId int IDENTITY PRIMARY KEY,Nombre nvarchar(40) NOT NULL UNIQUE);
IF OBJECT_ID('dbo.Usuarios') IS NULL CREATE TABLE dbo.Usuarios(UsuarioId int IDENTITY PRIMARY KEY,RolId int NOT NULL REFERENCES dbo.Roles(RolId),Nombre nvarchar(120) NOT NULL,Correo nvarchar(180) NOT NULL UNIQUE,PasswordHash varbinary(32) NOT NULL,EsAutor bit NOT NULL DEFAULT 0,Activo bit NOT NULL DEFAULT 1,FechaRegistro datetime2 NOT NULL DEFAULT sysdatetime());
IF OBJECT_ID('dbo.Generos') IS NULL CREATE TABLE dbo.Generos(GeneroId int IDENTITY PRIMARY KEY,Nombre nvarchar(80) NOT NULL UNIQUE);
IF OBJECT_ID('dbo.Libros') IS NULL CREATE TABLE dbo.Libros(LibroId int IDENTITY PRIMARY KEY,Codigo varchar(20) NOT NULL UNIQUE,AutorId int NOT NULL REFERENCES dbo.Usuarios(UsuarioId),GeneroId int NOT NULL REFERENCES dbo.Generos(GeneroId),Titulo nvarchar(180) NOT NULL,Idioma nvarchar(40) NOT NULL,Descripcion nvarchar(2000) NOT NULL DEFAULT '',Precio decimal(10,2) NOT NULL DEFAULT 0,Valoracion decimal(3,2) NOT NULL DEFAULT 0,PortadaUrl nvarchar(500) NOT NULL DEFAULT '',RutaPdf nvarchar(500) NULL,Contenido nvarchar(max) NULL,Estado nvarchar(30) NOT NULL DEFAULT N'Publicado',FechaPublicacion date NOT NULL DEFAULT CAST(getdate() AS date),Ventas int NOT NULL DEFAULT 0,Descargas int NOT NULL DEFAULT 0);
IF OBJECT_ID('dbo.Biblioteca') IS NULL CREATE TABLE dbo.Biblioteca(UsuarioId int NOT NULL REFERENCES dbo.Usuarios(UsuarioId),LibroId int NOT NULL REFERENCES dbo.Libros(LibroId),FechaAdquisicion datetime2 NOT NULL DEFAULT sysdatetime(),Leido bit NOT NULL DEFAULT 0,CONSTRAINT PK_Biblioteca PRIMARY KEY(UsuarioId,LibroId));
IF OBJECT_ID('dbo.Carrito') IS NULL CREATE TABLE dbo.Carrito(UsuarioId int NOT NULL REFERENCES dbo.Usuarios(UsuarioId),LibroId int NOT NULL REFERENCES dbo.Libros(LibroId),Fecha datetime2 NOT NULL DEFAULT sysdatetime(),CONSTRAINT PK_Carrito PRIMARY KEY(UsuarioId,LibroId));
IF OBJECT_ID('dbo.Compras') IS NULL CREATE TABLE dbo.Compras(CompraId int IDENTITY PRIMARY KEY,Codigo varchar(24) NOT NULL UNIQUE,UsuarioId int NOT NULL REFERENCES dbo.Usuarios(UsuarioId),Fecha datetime2 NOT NULL DEFAULT sysdatetime(),Total decimal(10,2) NOT NULL,Estado nvarchar(30) NOT NULL DEFAULT N'Completada');
IF OBJECT_ID('dbo.CompraDetalle') IS NULL CREATE TABLE dbo.CompraDetalle(CompraDetalleId int IDENTITY PRIMARY KEY,CompraId int NOT NULL REFERENCES dbo.Compras(CompraId),LibroId int NOT NULL REFERENCES dbo.Libros(LibroId),Precio decimal(10,2) NOT NULL);
IF COL_LENGTH('dbo.Compras','Codigo')<40 ALTER TABLE dbo.Compras ALTER COLUMN Codigo varchar(40) NOT NULL;
IF OBJECT_ID('dbo.Sesiones') IS NULL CREATE TABLE dbo.Sesiones(SesionId bigint IDENTITY PRIMARY KEY,UsuarioId int NOT NULL REFERENCES dbo.Usuarios(UsuarioId),Inicio datetime2 NOT NULL DEFAULT sysdatetime(),Fin datetime2 NULL,DireccionIp varchar(48) NULL);
IF COL_LENGTH('dbo.Libros','NumeroPaginas') IS NULL ALTER TABLE dbo.Libros ADD NumeroPaginas int NULL;
IF COL_LENGTH('dbo.Libros','Formato') IS NULL ALTER TABLE dbo.Libros ADD Formato nvarchar(30) NOT NULL CONSTRAINT DF_Libros_Formato DEFAULT N'PDF';
IF OBJECT_ID('dbo.PreferenciasUsuario') IS NULL CREATE TABLE dbo.PreferenciasUsuario(UsuarioId int NOT NULL PRIMARY KEY REFERENCES dbo.Usuarios(UsuarioId),Genero nvarchar(80) NOT NULL,Paginas nvarchar(40) NOT NULL,Idioma nvarchar(40) NOT NULL,Formato nvarchar(40) NOT NULL,Ritmo nvarchar(40) NOT NULL,Ambiente nvarchar(40) NOT NULL,Descubrimiento nvarchar(40) NOT NULL,Actualizado datetime2 NOT NULL DEFAULT sysdatetime());
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Roles) INSERT dbo.Roles(Nombre) VALUES(N'Administrador'),(N'Usuario');
IF NOT EXISTS(SELECT 1 FROM dbo.Generos) INSERT dbo.Generos(Nombre) VALUES(N'Aventura'),(N'Ciencia Ficción'),(N'Drama'),(N'Ficción'),(N'Filosofía'),(N'Historia'),(N'No Ficción'),(N'Poesía'),(N'Romance'),(N'Thriller');
IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE Correo='admin@bookmatch.com')
BEGIN
 INSERT dbo.Usuarios(RolId,Nombre,Correo,PasswordHash,EsAutor,FechaRegistro) SELECT RolId,N'Administrador General','admin@bookmatch.com',HASHBYTES('SHA2_256',CONVERT(nvarchar(4000),N'password123')),0,'2024-01-09' FROM dbo.Roles WHERE Nombre=N'Administrador';
 INSERT dbo.Usuarios(RolId,Nombre,Correo,PasswordHash,EsAutor,FechaRegistro) SELECT RolId,N'Elena Vargas','elena@example.com',HASHBYTES('SHA2_256',CONVERT(nvarchar(4000),N'password123')),1,'2024-03-14' FROM dbo.Roles WHERE Nombre=N'Usuario';
 INSERT dbo.Usuarios(RolId,Nombre,Correo,PasswordHash,EsAutor,FechaRegistro) SELECT RolId,N'Carlos Mendoza','carlos@example.com',HASHBYTES('SHA2_256',CONVERT(nvarchar(4000),N'password123')),1,'2024-04-01' FROM dbo.Roles WHERE Nombre=N'Usuario';
 INSERT dbo.Usuarios(RolId,Nombre,Correo,PasswordHash,EsAutor,Activo,FechaRegistro) SELECT RolId,N'Sofía Reyes','sofia@example.com',HASHBYTES('SHA2_256',CONVERT(nvarchar(4000),N'password123')),0,0,'2024-05-19' FROM dbo.Roles WHERE Nombre=N'Usuario';
 INSERT dbo.Usuarios(RolId,Nombre,Correo,PasswordHash,EsAutor,FechaRegistro) SELECT RolId,N'Andrés Torres','andres@example.com',HASHBYTES('SHA2_256',CONVERT(nvarchar(4000),N'password123')),1,'2024-06-10' FROM dbo.Roles WHERE Nombre=N'Usuario';
 INSERT dbo.Usuarios(RolId,Nombre,Correo,PasswordHash,EsAutor,FechaRegistro) SELECT RolId,N'Isabella Cruz','isabella@example.com',HASHBYTES('SHA2_256',CONVERT(nvarchar(4000),N'password123')),1,'2024-07-03' FROM dbo.Roles WHERE Nombre=N'Usuario';
END
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Libros)
BEGIN
 DECLARE @elena int=(SELECT UsuarioId FROM dbo.Usuarios WHERE Correo='elena@example.com'),@carlos int=(SELECT UsuarioId FROM dbo.Usuarios WHERE Correo='carlos@example.com'),@andres int=(SELECT UsuarioId FROM dbo.Usuarios WHERE Correo='andres@example.com'),@isabella int=(SELECT UsuarioId FROM dbo.Usuarios WHERE Correo='isabella@example.com');
 INSERT dbo.Libros(Codigo,AutorId,GeneroId,Titulo,Idioma,Descripcion,Precio,Valoracion,PortadaUrl,FechaPublicacion,Ventas,Descargas) VALUES
 ('BM-001',@elena,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Ficción'),N'El Laberinto de los Espejos',N'Español',N'Una historia sobre identidad, memoria y caminos imposibles.',12.99,4.80,N'https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=800','2024-06-14',342,0),
 ('BM-002',@carlos,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Ciencia Ficción'),N'Código del Universo',N'Español',N'La humanidad descubre que el universo puede reescribirse.',0,4.70,N'https://images.unsplash.com/photo-1446776811953-b23d57bd21aa?w=800','2024-07-21',1204,1204),
 ('BM-003',@isabella,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Drama'),N'Raíces del Olvido',N'Español',N'Una familia recupera los secretos de varias generaciones.',9.99,4.50,N'https://images.unsplash.com/photo-1516979187457-637abb4f9353?w=800','2024-05-09',521,0),
 ('BM-004',@andres,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'No Ficción'),N'Mentes Brillantes',N'Español',N'Historias y hábitos de personas que transformaron el mundo.',14.99,4.90,N'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=800','2024-08-02',178,0),
 ('BM-005',@elena,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Romance'),N'Jardín de Medianoche',N'Español',N'Dos vidas se encuentran cada noche en un jardín secreto.',0,4.60,N'https://images.unsplash.com/photo-1497250681960-ef046c08a56e?w=800','2024-09-17',2891,2891),
 ('BM-006',@isabella,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Thriller'),N'El Último Algoritmo',N'Español',N'Una programadora descubre un sistema capaz de predecir delitos.',11.99,4.40,N'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=800','2024-10-19',411,0),
 ('BM-007',@andres,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Aventura'),N'Horizontes de Acero',N'Español',N'Una expedición imposible a través de territorios olvidados.',13.99,4.20,N'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?w=800','2024-11-03',34,0),
 ('BM-008',@carlos,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Historia'),N'Historia Viva de América',N'Español',N'Un recorrido accesible por hechos que definieron el continente.',7.99,4.30,N'https://images.unsplash.com/photo-1521295121783-8a321d551ad2?w=800','2024-11-30',85,0),
 ('BM-009',@elena,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Poesía'),N'Cartas a la Lluvia',N'Español',N'Poemas sobre memoria, ciudad y encuentros.',0,4.80,N'https://images.unsplash.com/photo-1455390582262-044cdead277a?w=800','2024-12-02',0,1332),
 ('BM-010',@isabella,(SELECT GeneroId FROM dbo.Generos WHERE Nombre=N'Filosofía'),N'El Arte de Preguntar',N'Español',N'Ideas para pensar mejor en tiempos inciertos.',10.50,4.60,N'https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=800','2024-12-08',91,0);
 DECLARE @u int=(SELECT UsuarioId FROM dbo.Usuarios WHERE Correo='elena@example.com');
 INSERT dbo.Biblioteca(UsuarioId,LibroId,FechaAdquisicion,Leido) SELECT @u,LibroId,DATEADD(day,-LibroId,sysdatetime()),CASE WHEN Codigo='BM-001' THEN 1 ELSE 0 END FROM dbo.Libros WHERE Codigo IN('BM-001','BM-004','BM-006');
 DECLARE @purchase int; INSERT dbo.Compras(Codigo,UsuarioId,Fecha,Total) VALUES('ORD-2024-001',@u,'2024-10-11',12.99);SET @purchase=SCOPE_IDENTITY();INSERT dbo.CompraDetalle(CompraId,LibroId,Precio) SELECT @purchase,LibroId,12.99 FROM dbo.Libros WHERE Codigo='BM-001';
END
GO
UPDATE dbo.Libros SET NumeroPaginas=CASE (LibroId%5) WHEN 0 THEN 120 WHEN 1 THEN 220 WHEN 2 THEN 340 WHEN 3 THEN 510 ELSE 280 END,Formato=CASE (LibroId%3) WHEN 0 THEN N'Online' WHEN 1 THEN N'PDF' ELSE N'EPUB' END WHERE NumeroPaginas IS NULL;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Auth_Login @Email nvarchar(180),@Password nvarchar(4000) AS
BEGIN SET NOCOUNT ON;SELECT u.UsuarioId,u.Nombre,u.Correo,r.Nombre Rol,u.EsAutor FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId=u.RolId WHERE u.Correo=@Email AND u.Activo=1 AND u.PasswordHash=HASHBYTES('SHA2_256',CONVERT(nvarchar(4000),@Password));END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_Obtener @UsuarioId int,@EsAdmin bit AS
BEGIN SET NOCOUNT ON;
 SELECT N'Libros Publicados' Etiqueta,CONVERT(nvarchar(30),COUNT(*)) Valor,N'Catálogo activo' Detalle,N'▤' Icono,N'purple' Tono FROM dbo.Libros UNION ALL
 SELECT N'Usuarios Registrados',CONVERT(nvarchar(30),COUNT(*)),N'Lectores y autores',N'♙',N'gold' FROM dbo.Usuarios UNION ALL
 SELECT N'Ingresos Totales',FORMAT(COALESCE(SUM(Total),0),'C','en-US'),N'Transacciones realizadas',N'$',N'blue' FROM dbo.Compras UNION ALL
 SELECT N'Descargas Gratuitas',CONVERT(nvarchar(30),COALESCE(SUM(Descargas),0)),N'Libros accedidos sin costo',N'↓',N'green' FROM dbo.Libros;
 ;WITH m(n,Mes) AS(SELECT 1,'Ene' UNION ALL SELECT 2,'Feb' UNION ALL SELECT 3,'Mar' UNION ALL SELECT 4,'Abr' UNION ALL SELECT 5,'May' UNION ALL SELECT 6,'Jun' UNION ALL SELECT 7,'Jul' UNION ALL SELECT 8,'Ago' UNION ALL SELECT 9,'Sep' UNION ALL SELECT 10,'Oct' UNION ALL SELECT 11,'Nov' UNION ALL SELECT 12,'Dic') SELECT Mes,CAST(900+n*325 AS decimal(10,2)) Ventas,1800+n*650 Descargas FROM m;
 SELECT g.Nombre Genero,COUNT(l.LibroId) Total FROM dbo.Generos g LEFT JOIN dbo.Libros l ON l.GeneroId=g.GeneroId GROUP BY g.Nombre HAVING COUNT(l.LibroId)>0 ORDER BY Total DESC;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Libro_Catalogo @Busqueda nvarchar(180)=NULL,@Genero nvarchar(80)=NULL,@Idioma nvarchar(40)=NULL,@TipoPrecio varchar(10)='all',@ValoracionMinima decimal(3,2)=NULL AS
BEGIN SET NOCOUNT ON;SELECT l.LibroId,l.Codigo,l.Titulo,u.Nombre Autor,g.Nombre Genero,l.Idioma,l.Descripcion,l.Precio,l.Valoracion,l.PortadaUrl,l.Estado,l.Ventas,l.Descargas,CAST(0 AS bit) Leido,CAST(l.FechaPublicacion AS datetime2) Fecha FROM dbo.Libros l JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId JOIN dbo.Generos g ON g.GeneroId=l.GeneroId WHERE l.Estado=N'Publicado' AND (@Busqueda IS NULL OR l.Titulo LIKE '%'+@Busqueda+'%' OR u.Nombre LIKE '%'+@Busqueda+'%') AND (@Genero IS NULL OR g.Nombre=@Genero) AND (@Idioma IS NULL OR l.Idioma=@Idioma) AND (@TipoPrecio='all' OR @TipoPrecio='free' AND l.Precio=0 OR @TipoPrecio='paid' AND l.Precio>0) AND (@ValoracionMinima IS NULL OR l.Valoracion>=@ValoracionMinima) ORDER BY l.FechaPublicacion DESC;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Biblioteca_Listar @UsuarioId int,@Filtro varchar(12)='all' AS
BEGIN SET NOCOUNT ON;SELECT l.LibroId,l.Codigo,l.Titulo,u.Nombre Autor,g.Nombre Genero,l.Idioma,l.Descripcion,l.Precio,l.Valoracion,l.PortadaUrl,l.Estado,l.Ventas,l.Descargas,b.Leido,b.FechaAdquisicion Fecha FROM dbo.Biblioteca b JOIN dbo.Libros l ON l.LibroId=b.LibroId JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId JOIN dbo.Generos g ON g.GeneroId=l.GeneroId WHERE b.UsuarioId=@UsuarioId AND (@Filtro='all' OR @Filtro='read' AND b.Leido=1 OR @Filtro='pending' AND b.Leido=0) ORDER BY b.FechaAdquisicion DESC;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Publicacion_Listar @UsuarioId int AS
BEGIN SET NOCOUNT ON;SELECT l.LibroId,l.Codigo,l.Titulo,u.Nombre Autor,g.Nombre Genero,l.Idioma,l.Descripcion,l.Precio,l.Valoracion,l.PortadaUrl,l.Estado,l.Ventas,l.Descargas,CAST(0 AS bit) Leido,CAST(l.FechaPublicacion AS datetime2) Fecha FROM dbo.Libros l JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId JOIN dbo.Generos g ON g.GeneroId=l.GeneroId WHERE l.AutorId=@UsuarioId ORDER BY l.FechaPublicacion DESC;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Libro_Publicar @UsuarioId int,@Titulo nvarchar(180),@Genero nvarchar(80),@Idioma nvarchar(40),@Precio decimal(10,2),@Descripcion nvarchar(2000),@Contenido nvarchar(max)=NULL,@RutaPdf nvarchar(500)=NULL AS
BEGIN SET NOCOUNT ON;DECLARE @GeneroId int=(SELECT GeneroId FROM dbo.Generos WHERE Nombre=@Genero);IF @GeneroId IS NULL THROW 50001,'Género no válido.',1;DECLARE @next int=ISNULL((SELECT MAX(LibroId) FROM dbo.Libros),0)+1;INSERT dbo.Libros(Codigo,AutorId,GeneroId,Titulo,Idioma,Descripcion,Precio,Valoracion,PortadaUrl,RutaPdf,Contenido,Estado) VALUES(CONCAT('BM-',FORMAT(@next,'000')),@UsuarioId,@GeneroId,@Titulo,@Idioma,ISNULL(@Descripcion,''),@Precio,0,N'https://images.unsplash.com/photo-1543002588-bfa74002ed7e?w=800',@RutaPdf,@Contenido,N'Publicado');SELECT CONVERT(int,SCOPE_IDENTITY());END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Carrito_Agregar @UsuarioId int,@LibroId int AS BEGIN SET NOCOUNT ON;IF EXISTS(SELECT 1 FROM dbo.Libros WHERE LibroId=@LibroId AND Precio=0) BEGIN IF NOT EXISTS(SELECT 1 FROM dbo.Biblioteca WHERE UsuarioId=@UsuarioId AND LibroId=@LibroId) INSERT dbo.Biblioteca(UsuarioId,LibroId)VALUES(@UsuarioId,@LibroId);UPDATE dbo.Libros SET Descargas=Descargas+1 WHERE LibroId=@LibroId;RETURN;END IF NOT EXISTS(SELECT 1 FROM dbo.Carrito WHERE UsuarioId=@UsuarioId AND LibroId=@LibroId) INSERT dbo.Carrito(UsuarioId,LibroId)VALUES(@UsuarioId,@LibroId);END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Carrito_Listar @UsuarioId int AS BEGIN SET NOCOUNT ON;SELECT l.LibroId,l.Titulo,u.Nombre Autor,l.Precio FROM dbo.Carrito c JOIN dbo.Libros l ON l.LibroId=c.LibroId JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId WHERE c.UsuarioId=@UsuarioId;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Carrito_Eliminar @UsuarioId int,@LibroId int AS BEGIN DELETE dbo.Carrito WHERE UsuarioId=@UsuarioId AND LibroId=@LibroId;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Carrito_Comprar @UsuarioId int AS
BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;BEGIN TRAN;DECLARE @Total decimal(10,2)=(SELECT COALESCE(SUM(l.Precio),0) FROM dbo.Carrito c JOIN dbo.Libros l ON l.LibroId=c.LibroId WHERE c.UsuarioId=@UsuarioId);IF @Total=0 BEGIN ROLLBACK;RETURN;END DECLARE @Codigo varchar(40)=CONCAT('ORD-',FORMAT(GETDATE(),'yyyyMMddHHmmss'),'-',@UsuarioId,'-',LEFT(REPLACE(CONVERT(varchar(36),NEWID()),'-',''),8));INSERT dbo.Compras(Codigo,UsuarioId,Total)VALUES(@Codigo,@UsuarioId,@Total);DECLARE @CompraId int=SCOPE_IDENTITY();INSERT dbo.CompraDetalle(CompraId,LibroId,Precio) SELECT @CompraId,l.LibroId,l.Precio FROM dbo.Carrito c JOIN dbo.Libros l ON l.LibroId=c.LibroId WHERE c.UsuarioId=@UsuarioId;INSERT dbo.Biblioteca(UsuarioId,LibroId) SELECT @UsuarioId,c.LibroId FROM dbo.Carrito c WHERE c.UsuarioId=@UsuarioId AND NOT EXISTS(SELECT 1 FROM dbo.Biblioteca b WHERE b.UsuarioId=@UsuarioId AND b.LibroId=c.LibroId);UPDATE l SET Ventas=Ventas+1 FROM dbo.Libros l JOIN dbo.Carrito c ON c.LibroId=l.LibroId WHERE c.UsuarioId=@UsuarioId;DELETE dbo.Carrito WHERE UsuarioId=@UsuarioId;COMMIT;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Consulta_Compras AS BEGIN SET NOCOUNT ON;SELECT c.Codigo,u.Nombre Usuario,l.Titulo Libro,a.Nombre Autor,c.Fecha,d.Precio Monto,CASE WHEN b.Leido=1 THEN N'Leído' ELSE N'Pendiente' END Estado FROM dbo.Compras c JOIN dbo.Usuarios u ON u.UsuarioId=c.UsuarioId JOIN dbo.CompraDetalle d ON d.CompraId=c.CompraId JOIN dbo.Libros l ON l.LibroId=d.LibroId JOIN dbo.Usuarios a ON a.UsuarioId=l.AutorId LEFT JOIN dbo.Biblioteca b ON b.UsuarioId=c.UsuarioId AND b.LibroId=l.LibroId ORDER BY c.Fecha DESC;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Usuario_Listar @Busqueda nvarchar(180)=NULL AS BEGIN SET NOCOUNT ON;SELECT u.UsuarioId,u.Nombre,u.Correo,r.Nombre Rol,COUNT(l.LibroId) Publicados,u.FechaRegistro Registrado,u.Activo FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId=u.RolId LEFT JOIN dbo.Libros l ON l.AutorId=u.UsuarioId WHERE @Busqueda IS NULL OR u.Nombre LIKE '%'+@Busqueda+'%' OR u.Correo LIKE '%'+@Busqueda+'%' GROUP BY u.UsuarioId,u.Nombre,u.Correo,r.Nombre,u.FechaRegistro,u.Activo ORDER BY u.FechaRegistro;END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Usuario_Estado @UsuarioId int,@Activo bit AS BEGIN UPDATE dbo.Usuarios SET Activo=@Activo WHERE UsuarioId=@UsuarioId AND RolId<>(SELECT RolId FROM dbo.Roles WHERE Nombre=N'Administrador');END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Preferencia_Guardar @UsuarioId int,@Genero nvarchar(80),@Paginas nvarchar(40),@Idioma nvarchar(40),@Formato nvarchar(40),@Ritmo nvarchar(40),@Ambiente nvarchar(40),@Descubrimiento nvarchar(40) AS
BEGIN SET NOCOUNT ON;UPDATE dbo.PreferenciasUsuario SET Genero=@Genero,Paginas=@Paginas,Idioma=@Idioma,Formato=@Formato,Ritmo=@Ritmo,Ambiente=@Ambiente,Descubrimiento=@Descubrimiento,Actualizado=sysdatetime() WHERE UsuarioId=@UsuarioId;IF @@ROWCOUNT=0 INSERT dbo.PreferenciasUsuario(UsuarioId,Genero,Paginas,Idioma,Formato,Ritmo,Ambiente,Descubrimiento)VALUES(@UsuarioId,@Genero,@Paginas,@Idioma,@Formato,@Ritmo,@Ambiente,@Descubrimiento);END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Recomendacion_Obtener @UsuarioId int AS
BEGIN SET NOCOUNT ON;DECLARE @Genero nvarchar(80),@Paginas nvarchar(40),@Idioma nvarchar(40),@Formato nvarchar(40),@Ritmo nvarchar(40),@Ambiente nvarchar(40),@Descubrimiento nvarchar(40);SELECT @Genero=Genero,@Paginas=Paginas,@Idioma=Idioma,@Formato=Formato,@Ritmo=Ritmo,@Ambiente=Ambiente,@Descubrimiento=Descubrimiento FROM dbo.PreferenciasUsuario WHERE UsuarioId=@UsuarioId;
 SELECT l.LibroId,l.Codigo,l.Titulo,u.Nombre Autor,g.Nombre Genero,l.Idioma,l.Descripcion,l.Precio,l.Valoracion,l.PortadaUrl,l.Estado,l.Ventas,l.Descargas,CAST(0 AS bit) Leido,CAST(l.FechaPublicacion AS datetime2) Fecha,
 CAST((CASE WHEN g.Nombre=@Genero THEN 30 ELSE 0 END)+(CASE WHEN l.Idioma=@Idioma THEN 15 ELSE 0 END)+(CASE WHEN @Formato=N'Sin preferencia' OR l.Formato LIKE CASE WHEN @Formato LIKE N'%PDF%' THEN N'%PDF%' WHEN @Formato LIKE N'%línea%' THEN N'%Online%' WHEN @Formato LIKE N'%Epub%' THEN N'%EPUB%' ELSE N'%' END THEN 15 ELSE 0 END)+(CASE WHEN @Paginas=N'No tengo preferencia' OR @Paginas LIKE N'Menos%' AND l.NumeroPaginas<150 OR @Paginas=N'150–300 páginas' AND l.NumeroPaginas BETWEEN 150 AND 300 OR @Paginas=N'300–500 páginas' AND l.NumeroPaginas BETWEEN 301 AND 500 OR @Paginas LIKE N'Más%' AND l.NumeroPaginas>500 THEN 10 ELSE 0 END)+(CASE WHEN @Ritmo=N'Ágil y dinámico' AND g.Nombre IN(N'Thriller',N'Aventura',N'Ciencia Ficción') OR @Ritmo=N'Lento y reflexivo' AND g.Nombre IN(N'Filosofía',N'Poesía',N'Historia') OR @Ritmo=N'Equilibrado' THEN 10 ELSE 0 END)+(CASE WHEN @Ambiente=N'Misterioso' AND g.Nombre=N'Thriller' OR @Ambiente=N'Romántico' AND g.Nombre=N'Romance' OR @Ambiente=N'Épico' AND g.Nombre IN(N'Aventura',N'Ciencia Ficción') OR @Ambiente=N'Inspirador' AND g.Nombre IN(N'No Ficción',N'Poesía') OR @Ambiente=N'Realista' AND g.Nombre IN(N'Drama',N'Historia') THEN 10 ELSE 0 END)+(CASE WHEN @Descubrimiento=N'Tendencias' AND l.Ventas>=500 OR @Descubrimiento=N'Recomendaciones' AND l.Valoracion>=4.5 OR @Descubrimiento=N'Explorando géneros' AND g.Nombre<>@Genero OR @Descubrimiento=N'Autores favoritos' AND EXISTS(SELECT 1 FROM dbo.Biblioteca b JOIN dbo.Libros lb ON lb.LibroId=b.LibroId WHERE b.UsuarioId=@UsuarioId AND lb.AutorId=l.AutorId) THEN 10 ELSE 0 END) AS int) Afinidad
 FROM dbo.Libros l JOIN dbo.Usuarios u ON u.UsuarioId=l.AutorId JOIN dbo.Generos g ON g.GeneroId=l.GeneroId WHERE l.Estado=N'Publicado' ORDER BY Afinidad DESC,l.Valoracion DESC,l.Ventas DESC;
END
GO
PRINT 'BookMatchDb instalada correctamente. Usuarios demo: admin@bookmatch.com y elena@example.com / password123';
