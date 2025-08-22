IF OBJECT_ID('[dbo].[ObtenerConsecutivoSeguro]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ObtenerConsecutivoSeguro]
    IF OBJECT_ID('[dbo].[ObtenerConsecutivoSeguro]') IS NOT NULL
        PRINT '<<< FAILED DROPPING PROCEDURE [dbo].[ObtenerConsecutivoSeguro] >>>'
    ELSE
        PRINT '<<< DROPPED PROCEDURE [dbo].[ObtenerConsecutivoSeguro] >>>'
END
GO
CREATE PROCEDURE [dbo].[ObtenerConsecutivoSeguro]
    @Clave			NVARCHAR(50),
    @Consecutivo	NUMERIC(12,0) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
	DECLARE 
	@incremento NUMERIC(12,0)
	
	SELECT @incremento = 1
	
    BEGIN TRAN;
    EXEC sp_getapplock 
        @Resource = @Clave,
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
		@LockTimeout = 1000; -- 10 segundos de espera

    -- Actualizar valor y retornar
	/*
    UPDATE Consecutivos
    SET Valor = Valor + 1
    WHERE Clave = @Clave;
    SELECT @Consecutivo = Valor FROM Consecutivos WHERE Clave = @Clave;
	*/
	
	UPDATE dbo.Consecutivos WITH (ROWLOCK, UPDLOCK, HOLDLOCK)
	SET Valor = Valor + @incremento, @Consecutivo= Valor + @incremento
	WHERE Clave = @Clave;

    COMMIT;
END
GO
IF EXISTS (
    SELECT 1
    FROM sysobjects
    WHERE id = OBJECT_ID('dbo.ObtenerConsecutivoSeguro')
    AND type = 'P'
)
BEGIN
    PRINT '<<CREATE PROCEDURE dbo.ObtenerConsecutivoSeguro SUCCESSFUL!! >>'
END
GO

GRANT EXECUTE ON [dbo].[ObtenerConsecutivoSeguro] TO [public];
GO