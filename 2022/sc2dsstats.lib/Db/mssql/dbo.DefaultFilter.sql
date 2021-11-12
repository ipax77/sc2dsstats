﻿GO
CREATE VIEW [dbo].[DefaultFilter]
	WITH SCHEMABINDING
	AS SELECT ID, GAMETIME, WINNER, DURATION, MAXKILLSUM, GAMEMODE  FROM [dbo].[DSReplays] AS [d]
	WHERE (((((((((([d].[DURATION] > '00:05:00')) AND ([d].[MAXLEAVER] < 2000)) AND ([d].[MINARMY] > 1500)) AND ([d].[MININCOME] > 1500)) AND ([d].[MINKILLSUM] > 1500)) AND ([d].[PLAYERCOUNT] = 6))

GO
CREATE UNIQUE CLUSTERED INDEX IDX_V1 ON [dbo].[DefaultFilter] (ID);

