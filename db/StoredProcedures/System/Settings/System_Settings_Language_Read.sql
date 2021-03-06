SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Neil O'Keeffe
-- Create date: 20/09/2018
-- Description:	Returns a Language entry based on its iso code. Returns all languages if an iso code isn't supplied
-- exec System_Settings_Language_Read 'ga'
-- =============================================
CREATE
	OR

ALTER PROCEDURE System_Settings_Language_Read @LngIsoCode CHAR(2) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	SELECT LNG_ISO_CODE AS LngIsoCode
		,LNG_ISO_NAME AS LngIsoName
		,CASE 
			WHEN RlsCount IS NULL
				THEN 0
			ELSE RlsCount
			END AS RlsCount
	FROM TS_LANGUAGE
	LEFT JOIN (
		SELECT MTR_LNG_ID
			,count(*) AS RlsCount
		FROM TD_MATRIX
		INNER JOIN TD_RELEASE
			ON MTR_RLS_ID = RLS_ID
		WHERE MTR_DELETE_FLAG = 0
			AND RLS_DELETE_FLAG = 0
		GROUP BY MTR_LNG_ID
		) RLS
		ON RLS.MTR_LNG_ID = LNG_ID
	WHERE LNG_DELETE_FLAG = 0
		AND (
			@LngIsoCode IS NULL
			OR (LNG_ISO_CODE = @LngIsoCode)
			)
END
