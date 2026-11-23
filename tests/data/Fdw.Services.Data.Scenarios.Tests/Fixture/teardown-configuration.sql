-- FDW-6365 scenario fixture: removes the four container declarations, their keys and their fields, in ConfigurationDb.
-- Run:  sqlcmd -S <server> -d <ConfigurationDb> -b -I -v DataStore=<data store> Path=<path> OldDataStore=<..> OldPath=<..> OldContainer=<..> -i teardown-configuration.sql
--   OldDataStore, OldPath, OldContainer: the values setup-configuration.sql printed as 'OLD <name>=<value>', or the marker (none) for a setting that did not
--   exist (the teardown then deletes the rows the setup added). All five are required and have no default; a real value of exactly (none) cannot be restored.
-- Deletes only the fields and containers named Teams, Players, ScenarioValues and ScenarioDataOption under that store and path. It also puts the three DataOptions server settings
-- back as the setup found them (see the Old* variables). Restart the host afterwards.
-- NOT RUN by its author.
-- Dev only (Release-Supervisor's decision, 2026-11-18): refuses to run on any database but devConfigurationDb. sqlcmd -b stops at the THROW.
SET NOCOUNT ON;
IF DB_NAME() <> N'devConfigurationDb' THROW 50001, N'teardown-configuration.sql runs only on devConfigurationDb', 1;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- The keys hang off the containers: their key fields first, then the keys, then the fields and the containers below.
DELETE kf
FROM data.DataContainerKeyField kf
JOIN data.DataContainerKey k ON k.RowId = kf.DataContainerKeyRowId
JOIN data.DataContainer c ON c.RowId = k.DataContainerRowId
JOIN data.DataPath p ON p.RowId = c.DataPathRowId AND p.Name = N'$(Path)'
JOIN data.DataStoreConfiguration sc ON sc.RowId = p.DataStoreConfigurationRowId
JOIN data.DataStore s ON s.RowId = sc.DataStoreRowId AND s.Name = N'$(DataStore)'
WHERE c.Name IN (N'Teams', N'Players', N'ScenarioValues', N'ScenarioDataOption');
PRINT 'key fields removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
DELETE k
FROM data.DataContainerKey k
JOIN data.DataContainer c ON c.RowId = k.DataContainerRowId
JOIN data.DataPath p ON p.RowId = c.DataPathRowId AND p.Name = N'$(Path)'
JOIN data.DataStoreConfiguration sc ON sc.RowId = p.DataStoreConfigurationRowId
JOIN data.DataStore s ON s.RowId = sc.DataStoreRowId AND s.Name = N'$(DataStore)'
WHERE c.Name IN (N'Teams', N'Players', N'ScenarioValues', N'ScenarioDataOption');
PRINT 'keys removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
DELETE f
FROM data.DataContainerField f
JOIN data.DataContainer c ON c.RowId = f.DataContainerRowId
JOIN data.DataPath p ON p.RowId = c.DataPathRowId AND p.Name = N'$(Path)'
JOIN data.DataStoreConfiguration sc ON sc.RowId = p.DataStoreConfigurationRowId
JOIN data.DataStore s ON s.RowId = sc.DataStoreRowId AND s.Name = N'$(DataStore)'
WHERE c.Name IN (N'Teams', N'Players', N'ScenarioValues', N'ScenarioDataOption');
PRINT 'fields removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
DELETE c
FROM data.DataContainer c
JOIN data.DataPath p ON p.RowId = c.DataPathRowId AND p.Name = N'$(Path)'
JOIN data.DataStoreConfiguration sc ON sc.RowId = p.DataStoreConfigurationRowId
JOIN data.DataStore s ON s.RowId = sc.DataStoreRowId AND s.Name = N'$(DataStore)'
WHERE c.Name IN (N'Teams', N'Players', N'ScenarioValues', N'ScenarioDataOption');
PRINT 'containers removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
-- The three DataOptions settings back as they were. A value that was (none) means the setting did not exist: delete the rows the setup added.
UPDATE i SET SettingValue = v.OldValue
FROM settings.ServerSettingConfiguration i
JOIN settings.ServerSetting d ON d.RowId = i.ServerSettingRowId AND d.IsCurrent = 1 AND d.IsDeleted = 0
JOIN (VALUES
     (N'DataOptionsDataStore', N'$(OldDataStore)')
    ,(N'DataOptionsPath',      N'$(OldPath)')
    ,(N'DataOptionsContainer', N'$(OldContainer)')
) v(SettingName, OldValue) ON v.SettingName = d.[Name]
WHERE i.IsCurrent = 1 AND i.IsDeleted = 0 AND v.OldValue <> N'(none)' AND i.SettingValue <> v.OldValue;
PRINT 'settings restored: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

DELETE i
FROM settings.ServerSettingConfiguration i
JOIN settings.ServerSetting d ON d.RowId = i.ServerSettingRowId
JOIN (VALUES (N'DataOptionsDataStore', N'$(OldDataStore)'), (N'DataOptionsPath', N'$(OldPath)'), (N'DataOptionsContainer', N'$(OldContainer)')) v(SettingName, OldValue)
  ON v.SettingName = d.[Name] AND v.OldValue = N'(none)';
PRINT 'setting values removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
DELETE d
FROM settings.ServerSetting d
JOIN (VALUES (N'DataOptionsDataStore', N'$(OldDataStore)'), (N'DataOptionsPath', N'$(OldPath)'), (N'DataOptionsContainer', N'$(OldContainer)')) v(SettingName, OldValue)
  ON v.SettingName = d.[Name] AND v.OldValue = N'(none)'
WHERE NOT EXISTS (SELECT 1 FROM settings.ServerSettingConfiguration x WHERE x.ServerSettingRowId = d.RowId);
PRINT 'settings removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
