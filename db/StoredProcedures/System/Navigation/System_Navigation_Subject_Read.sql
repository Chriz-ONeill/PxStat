SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Paulo Patricio
-- Read date: 11 Oct 2018
-- Description:	Reads record(s) from the TD_Subject table
--exec [System_Navigation_Subject_Read]  null,'ga','Eachnamaíoch'
-- =============================================
CREATE
	OR

ALTER PROCEDURE System_Navigation_Subject_Read @SbjCode INT = NULL
	,@LngIsoCode CHAR(2)
	,@SbjValue NVARCHAR(256) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @LngIsoId INT

	SET @LngIsoId = (
			SELECT LNG_ID
			FROM TS_LANGUAGE
			WHERE LNG_ISO_CODE = @LngIsoCode
				AND LNG_DELETE_FLAG = 0
			)

	SELECT SbjCode
		,SbjValue
		,sum((
				CASE 
					WHEN PRC_ID IS NOT NULL
						THEN 1
					ELSE 0
					END
				)) AS PrcCount
	FROM (
		SELECT SBJ_CODE AS SbjCode
			,coalesce(INNER_SLG.SLG_VALUE, SBJ_VALUE) AS SbjValue
			,PRC_ID
		FROM TD_SUBJECT
		LEFT JOIN (
			SELECT SLG_SBJ_ID
				,SLG_VALUE
			FROM TD_SUBJECT_LANGUAGE
			JOIN TS_LANGUAGE
				ON SLG_LNG_ID = LNG_ID
					AND LNG_DELETE_FLAG = 0
					AND LNG_ISO_CODE = @LngIsoCode
			) INNER_SLG
			ON SBJ_ID = INNER_SLG.SLG_SBJ_ID
		LEFT JOIN TD_PRODUCT
			ON PRC_SBJ_ID = SBJ_ID
				AND PRC_DELETE_FLAG = 0
		WHERE (
				(
					@SbjCode IS NOT NULL
					AND [SBJ_CODE] = @SbjCode
					)
				OR @SbjCode IS NULL
				)
			AND (
				(
					@SbjValue IS NOT NULL
					AND [SBJ_VALUE] = @SbjValue
					)
				OR @SbjValue IS NULL
				)
			AND SBJ_DELETE_FLAG = 0
		) INNER_SELECT
	GROUP BY SbjCode
		,SbjValue
END
