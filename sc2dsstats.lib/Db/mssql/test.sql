GO
IF OBJECT_ID ('[dbo].[DefaultFilter]', 'view') IS NOT NULL
   DROP VIEW [dbo].[DefaultFilter] ;
GO
CREATE VIEW [dbo].[DefaultFilter]
	WITH SCHEMABINDING
	AS SELECT ID, WINNER, MAXKILLSUM  FROM [dbo].[DSReplays] AS [d]
	WHERE (((((([d].[DURATION] > '00:05:00')) AND ([d].[MAXLEAVER] < 2000)) AND ([d].[MINARMY] > 1500)) AND ([d].[MININCOME] > 1500)) AND ([d].[MINKILLSUM] > 1500)) AND ([d].[PLAYERCOUNT] = 6);


GO
--Create an index on the view.
CREATE UNIQUE CLUSTERED INDEX IDX_V1
   ON [dbo].[DefaultFilter] (ID);