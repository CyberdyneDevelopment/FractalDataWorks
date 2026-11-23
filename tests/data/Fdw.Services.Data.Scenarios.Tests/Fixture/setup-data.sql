-- FDW-6365 scenario fixture, step 1 of 2: the tables, in the DATA database the host's data store points at (not ConfigurationDb).
-- Run:  sqlcmd -S <server> -d <data database> -b -I -v Schema=<schema> DatabaseUser=<principal> -i setup-data.sql
--   Schema        the schema that is the data path's name in ConfigurationDb (step 2 declares the containers under that path).
--   DatabaseUser  the database principal the host's connection to this data store signs in as. Granted rights on these tables only.
-- Both are required and have no default. Idempotent: a second run changes nothing. Never run against a database that holds real data in a schema
-- of the same name. NOT RUN by its author: written from the pattern of the route owner's proof scripts (.worktree-gate/proofs/FDW-5663-data-option-routes).
-- Dev only (Release-Supervisor's decision, 2026-11-18): refuses to run on any database but devDataDb. sqlcmd -b stops at the THROW.
SET NOCOUNT ON;
IF DB_NAME() <> N'devDataDb' THROW 50001, N'setup-data.sql runs only on devDataDb', 1;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF SCHEMA_ID(N'$(Schema)') IS NULL EXEC (N'CREATE SCHEMA [$(Schema)]');
GO
IF OBJECT_ID(N'[$(Schema)].Teams', N'U') IS NULL
    CREATE TABLE [$(Schema)].Teams
    (
         Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ScenarioTeams PRIMARY KEY
        ,Division    NVARCHAR(100)     NOT NULL
        ,Conference  NVARCHAR(100)     NOT NULL
    );
GO
IF OBJECT_ID(N'[$(Schema)].Players', N'U') IS NULL
    CREATE TABLE [$(Schema)].Players
    (
         Id    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ScenarioPlayers PRIMARY KEY
        ,Name  NVARCHAR(100)     NOT NULL
    );
GO
-- The column the values scenarios search: ColumnValues.feature's Background states exactly these values and row counts
-- (East 3, West 2, North 4, South 1, Eastern Shore 5) and checks them before every scenario.
IF OBJECT_ID(N'[$(Schema)].ScenarioValues', N'U') IS NULL
BEGIN
    CREATE TABLE [$(Schema)].ScenarioValues
    (
         Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ScenarioValues PRIMARY KEY
        ,Division  NVARCHAR(100)     NOT NULL
    );
    INSERT INTO [$(Schema)].ScenarioValues (Division)
    SELECT v.Division
    FROM (VALUES (N'East', 3), (N'West', 2), (N'North', 4), (N'South', 1), (N'Eastern Shore', 5)) v(Division, Copies)
    CROSS APPLY (SELECT TOP (v.Copies) 1 AS n FROM sys.all_objects) c;
END
GO
-- The options table the three DataOptions server settings name: an Id the application mints (no default), then the seven DataOptionRowFields.
IF OBJECT_ID(N'[$(Schema)].ScenarioDataOption', N'U') IS NULL
    CREATE TABLE [$(Schema)].ScenarioDataOption
    (
         Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ScenarioDataOption PRIMARY KEY
        ,DataStore   NVARCHAR(200)    NOT NULL
        ,Container   NVARCHAR(200)    NOT NULL
        ,[Column]    NVARCHAR(200)    NOT NULL
        ,[Value]     NVARCHAR(400)    NOT NULL
        ,OptionType  NVARCHAR(100)    NOT NULL
        ,[Option]    NVARCHAR(100)    NOT NULL
        ,Active      BIT              NOT NULL
    );
GO
GRANT SELECT ON [$(Schema)].Teams, [$(Schema)].Players, [$(Schema)].ScenarioValues TO [$(DatabaseUser)];
GRANT SELECT, INSERT, UPDATE, DELETE ON [$(Schema)].ScenarioDataOption TO [$(DatabaseUser)];
GO
SELECT N'ScenarioValues rows ' + CAST(COUNT(*) AS NVARCHAR(10)) + N' (expect 15), distinct ' + CAST(COUNT(DISTINCT Division) AS NVARCHAR(10)) + N' (expect 5)' FROM [$(Schema)].ScenarioValues;
GO
