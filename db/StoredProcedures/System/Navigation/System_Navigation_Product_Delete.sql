SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Paulo Patricio
-- Delete date: 11 Oct 2018
-- Description:	Deletes record(s) from the TD_Product table
-- =============================================
CREATE
	OR

ALTER PROCEDURE System_Navigation_Product_Delete @PrcCode NVARCHAR(32)
	,@userName NVARCHAR(256)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @eMessage VARCHAR(256)
	DECLARE @updateCount INT
	DECLARE @DtgID INT = NULL
	DECLARE @PrcID INT = NULL

	--check if record exists
	SELECT @PrcID = PRC_ID
		,@DtgID = PRC_DTG_ID
	FROM TD_PRODUCT
	WHERE PRC_CODE = @PrcCode
		AND PRC_DELETE_FLAG = 0

	IF @PrcID IS NULL
	BEGIN
		RETURN 0
	END

	-- check if is not being used in a release
	IF EXISTS (
			SELECT 1
			FROM TD_RELEASE
			WHERE RLS_PRC_ID = @PrcID
				AND RLS_DELETE_FLAG = 0
			)
	BEGIN
		SET @eMessage = 'SP: System_Navigation_Product_Delete - Product is being used in a release: ' + cast(isnull(@PrcCode, 0) AS VARCHAR)

		RAISERROR (
				@eMessage
				,16
				,1
				);

		RETURN 0
	END

	UPDATE [TD_PRODUCT]
	SET [PRC_DELETE_FLAG] = 1
	WHERE PRC_ID = @PrcID
		AND PRC_DELETE_FLAG = 0

	SET @updateCount = @@ROWCOUNT

	IF @updateCount > 0
	BEGIN
		-- do the auditing 
		-- Create the entry in the TD_AUDITING table
		DECLARE @AuditUpdateCount INT

		EXEC @AuditUpdateCount = Security_Auditing_Delete @DtgID
			,@userName

		-- check the previous stored procedure for error
		IF @AuditUpdateCount = 0
		BEGIN
			SET @eMessage = 'Error creating entry in TD_AUDITING for Product delete:' + cast(isnull(@PrcCode, 0) AS VARCHAR)

			RAISERROR (
					@eMessage
					,16
					,1
					)

			RETURN
		END
	END

	-- Return the number of rows deleted
	RETURN @updateCount
END
GO


