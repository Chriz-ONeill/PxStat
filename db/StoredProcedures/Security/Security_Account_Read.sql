SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Neil O'Keeffe
-- Create date: 02/10/2018
-- Description:	Read Account details
-- =============================================
CREATE
	OR

ALTER PROCEDURE Security_Account_Read @CcnUsername NVARCHAR(256) = NULL
	,@PrvCode NVARCHAR(32) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	SELECT ccn.CCN_USERNAME AS CcnUsername
		,prv.PRV_CODE AS PrvCode
		,prv.PRV_VALUE AS PrvValue
		,ccn.CCN_NOTIFICATION_FLAG as CcnNotificationFlag
	FROM TD_ACCOUNT ccn
	INNER JOIN TS_PRIVILEGE prv
		ON ccn.CCN_PRV_ID = prv.PRV_ID
			AND ccn.CCN_DELETE_FLAG = 0
	WHERE (
			@CcnUsername IS NULL
			OR ccn.CCN_USERNAME = @CcnUsername
			)
		AND (
			@PrvCode IS NULL
			OR prv.PRV_CODE = @PrvCode
			)
END
