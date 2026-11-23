-- FDW-6365 scenario fixture: removes the four tables setup-data.sql created, in the DATA database. The schema is left in place.
-- Run:  sqlcmd -S <server> -d <data database> -b -I -v Schema=<schema> -i teardown-data.sql
-- Drops only Teams, Players, ScenarioValues and ScenarioDataOption of that schema; nothing else. NOT RUN by its author.
-- Dev only (Release-Supervisor's decision, 2026-11-18): refuses to run on any database but devDataDb. sqlcmd -b stops at the THROW.
SET NOCOUNT ON;
IF DB_NAME() <> N'devDataDb' THROW 50001, N'teardown-data.sql runs only on devDataDb', 1;
GO
DROP TABLE IF EXISTS [$(Schema)].ScenarioDataOption;
DROP TABLE IF EXISTS [$(Schema)].ScenarioValues;
DROP TABLE IF EXISTS [$(Schema)].Players;
DROP TABLE IF EXISTS [$(Schema)].Teams;
GO
