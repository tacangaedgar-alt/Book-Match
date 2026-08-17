/* Ejecutar una vez sobre una base BookMatchDb existente. No elimina datos. */
USE BookMatchDb;
GO

IF COL_LENGTH('dbo.Compras','MetodoPago') IS NULL ALTER TABLE dbo.Compras ADD MetodoPago nvarchar(20) NULL;
IF COL_LENGTH('dbo.Compras','ReferenciaPago') IS NULL ALTER TABLE dbo.Compras ADD ReferenciaPago nvarchar(180) NULL;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Carrito_Comprar
 @UsuarioId int,
 @MetodoPago nvarchar(20),
 @ReferenciaPago nvarchar(180)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @MetodoPago IS NULL OR @MetodoPago NOT IN(N'Card',N'PayPal') OR NULLIF(LTRIM(RTRIM(@ReferenciaPago)),N'') IS NULL THROW 50005,'Método de pago no válido.',1;
 BEGIN TRAN;
 DECLARE @Total decimal(10,2)=(SELECT COALESCE(SUM(l.Precio),0) FROM dbo.Carrito c JOIN dbo.Libros l ON l.LibroId=c.LibroId WHERE c.UsuarioId=@UsuarioId);
 IF @Total=0 BEGIN ROLLBACK;RETURN;END
 DECLARE @Codigo varchar(40)=CONCAT('ORD-',FORMAT(GETDATE(),'yyyyMMddHHmmss'),'-',@UsuarioId,'-',LEFT(REPLACE(CONVERT(varchar(36),NEWID()),'-',''),8));
 INSERT dbo.Compras(Codigo,UsuarioId,Total,MetodoPago,ReferenciaPago) VALUES(@Codigo,@UsuarioId,@Total,@MetodoPago,@ReferenciaPago);
 DECLARE @CompraId int=SCOPE_IDENTITY();
 INSERT dbo.CompraDetalle(CompraId,LibroId,Precio) SELECT @CompraId,l.LibroId,l.Precio FROM dbo.Carrito c JOIN dbo.Libros l ON l.LibroId=c.LibroId WHERE c.UsuarioId=@UsuarioId;
 INSERT dbo.Biblioteca(UsuarioId,LibroId) SELECT @UsuarioId,c.LibroId FROM dbo.Carrito c WHERE c.UsuarioId=@UsuarioId AND NOT EXISTS(SELECT 1 FROM dbo.Biblioteca b WHERE b.UsuarioId=@UsuarioId AND b.LibroId=c.LibroId);
 UPDATE l SET Ventas=Ventas+1 FROM dbo.Libros l JOIN dbo.Carrito c ON c.LibroId=l.LibroId WHERE c.UsuarioId=@UsuarioId;
 DELETE dbo.Carrito WHERE UsuarioId=@UsuarioId;
 COMMIT;
END
GO

IF OBJECT_ID('dbo.usp_Carrito_Comprar','P') IS NULL THROW 50006,'No se pudo instalar el checkout.',1;
PRINT 'Checkout simulado instalado correctamente.';
