-- FDW-6365 scenario fixture, step 2 of 2: declares the four containers and the three DataOptions settings, in ConfigurationDb.
-- Run:  sqlcmd -S <server> -d <ConfigurationDb> -b -I -v DataStore=<data store> Path=<path> -i setup-configuration.sql
--   DataStore  the name of an EXISTING data store whose connection reaches the data database where setup-data.sql ran.
--   Path       the name of an EXISTING path of that data store; it must equal the Schema given to setup-data.sql.
-- Both are required and have no default; this script creates neither the store nor the path and does not touch any connection. It declares
-- Teams, Players, ScenarioValues and ScenarioDataOption as containers of that path, then adds the three server settings that name
-- ScenarioDataOption. Idempotent. The three settings are PRINTED first as lines 'OLD <name>=<value>' ('(none)' when the setting did not exist) and then
-- set to the scenario fixture's values, so the scenarios never write into a table the host already names. KEEP THOSE THREE LINES: teardown-configuration.sql
-- needs them as -v OldDataStore=.. OldPath=.. OldContainer=.. (the value, or the marker (none) to have the teardown delete the rows this script added).
-- Restart the host after this script and after the teardown: settings and container declarations are cached.
-- The keys follow databases-seed seed 38 (the Surrogate key on Id). Written from the declaration pattern of the route owner's proof scripts, copying its column list and its DatumTypeId and ClassificationId
-- values unchanged. NOT RUN by its author; the first run on a scratch ConfigurationDb is the test.
-- Dev only (Release-Supervisor's decision, 2026-11-18): refuses to run on any database but devConfigurationDb. sqlcmd -b stops at the THROW.
SET NOCOUNT ON;
IF DB_NAME() <> N'devConfigurationDb' THROW 50001, N'setup-configuration.sql runs only on devConfigurationDb', 1;
GO
SET QUOTED_IDENTIFIER ON;
GO
INSERT INTO data.DataContainer (Id, DataPathRowId, Name, TypeId, ClassificationId, IsCurrent, IsDeleted)
SELECT NEWID(), p.RowId, v.ContainerName, 'Table', 470168002, 1, 0
FROM (VALUES (N'Teams'), (N'Players'), (N'ScenarioValues'), (N'ScenarioDataOption')) v(ContainerName)
JOIN data.DataStore s ON s.Name = N'$(DataStore)' AND s.IsCurrent = 1 AND s.IsDeleted = 0
JOIN data.DataPath p ON p.DataStoreConfigurationRowId = (SELECT RowId FROM data.DataStoreConfiguration WHERE DataStoreRowId = s.RowId AND IsCurrent = 1 AND IsDeleted = 0)
                    AND p.Name = N'$(Path)' AND p.IsCurrent = 1 AND p.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM data.DataContainer x WHERE x.DataPathRowId = p.RowId AND x.Name = v.ContainerName AND x.IsCurrent = 1 AND x.IsDeleted = 0);
PRINT 'containers declared: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' (expect 4 on a first run, 0 on a second)';
GO
INSERT INTO data.DataContainerField (Id, DataContainerRowId, Name, DataType, IsNullable, Ordinal, MaxLength, IsSystemProvided, FrameworkDataType, DatumTypeId, ClassificationId, IsCurrent, IsDeleted)
SELECT NEWID(), c.RowId, f.Name, f.DataType, 0, f.Ordinal, f.MaxLength, 0, f.FrameworkType, 1336224043, 470168002, 1, 0
FROM (VALUES
     (N'Teams',              N'Id',         N'int',              1, CAST(NULL AS INT), N'Int32')
    ,(N'Teams',              N'Division',   N'nvarchar',         2, 100,  N'String')
    ,(N'Teams',              N'Conference', N'nvarchar',         3, 100,  N'String')
    ,(N'Players',            N'Id',         N'int',              1, NULL, N'Int32')
    ,(N'Players',            N'Name',       N'nvarchar',         2, 100,  N'String')
    ,(N'ScenarioValues',     N'Id',         N'int',              1, NULL, N'Int32')
    ,(N'ScenarioValues',     N'Division',   N'nvarchar',         2, 100,  N'String')
    ,(N'ScenarioDataOption', N'Id',         N'uniqueidentifier', 1, NULL, N'Guid')
    ,(N'ScenarioDataOption', N'DataStore',  N'nvarchar',         2, 200,  N'String')
    ,(N'ScenarioDataOption', N'Container',  N'nvarchar',         3, 200,  N'String')
    ,(N'ScenarioDataOption', N'Column',     N'nvarchar',         4, 200,  N'String')
    ,(N'ScenarioDataOption', N'Value',      N'nvarchar',         5, 400,  N'String')
    ,(N'ScenarioDataOption', N'OptionType', N'nvarchar',         6, 100,  N'String')
    ,(N'ScenarioDataOption', N'Option',     N'nvarchar',         7, 100,  N'String')
    ,(N'ScenarioDataOption', N'Active',     N'bit',              8, NULL, N'Bool')
) f(ContainerName, Name, DataType, Ordinal, MaxLength, FrameworkType)
JOIN data.DataStore s ON s.Name = N'$(DataStore)' AND s.IsCurrent = 1 AND s.IsDeleted = 0
JOIN data.DataPath p ON p.DataStoreConfigurationRowId = (SELECT RowId FROM data.DataStoreConfiguration WHERE DataStoreRowId = s.RowId AND IsCurrent = 1 AND IsDeleted = 0)
                    AND p.Name = N'$(Path)' AND p.IsCurrent = 1 AND p.IsDeleted = 0
JOIN data.DataContainer c ON c.DataPathRowId = p.RowId AND c.Name = f.ContainerName AND c.IsCurrent = 1 AND c.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM data.DataContainerField x WHERE x.DataContainerRowId = c.RowId AND x.Name = f.Name AND x.IsCurrent = 1 AND x.IsDeleted = 0);
PRINT 'fields declared: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' (expect 15 on a first run, 0 on a second)';
GO
-- Keys: a container that has fields and no key is refused by every paged read (databases-seed seed 38, FDW-3454: "the fallback is gone"), and the values route and
-- the rows list both page. So each container declares the Surrogate key on its Id, the same two idempotent inserts as seed 38 (the key first, then its key field,
-- found by the key's name) with this fixture's container list.
INSERT INTO data.DataContainerKey (Id, DataContainerRowId, Name, TypeId, IsCurrent, IsDeleted)
SELECT NEWID(), c.RowId, v.KeyName, 'Surrogate', 1, 0
FROM (VALUES
     (N'Teams',              N'PK_Scenario_Teams')
    ,(N'Players',            N'PK_Scenario_Players')
    ,(N'ScenarioValues',     N'PK_Scenario_ScenarioValues')
    ,(N'ScenarioDataOption', N'PK_Scenario_ScenarioDataOption')
) v(ContainerName, KeyName)
JOIN data.DataStore s ON s.Name = N'$(DataStore)' AND s.IsCurrent = 1 AND s.IsDeleted = 0
JOIN data.DataPath p ON p.DataStoreConfigurationRowId = (SELECT RowId FROM data.DataStoreConfiguration WHERE DataStoreRowId = s.RowId AND IsCurrent = 1 AND IsDeleted = 0)
                    AND p.Name = N'$(Path)' AND p.IsCurrent = 1 AND p.IsDeleted = 0
JOIN data.DataContainer c ON c.DataPathRowId = (SELECT RowId FROM data.DataPath WHERE Id = p.Id AND IsCurrent = 1 AND IsDeleted = 0) AND c.Name = v.ContainerName AND c.IsCurrent = 1 AND c.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM data.DataContainerKey existing
    WHERE existing.DataContainerRowId = c.RowId AND existing.Name = v.KeyName AND existing.IsCurrent = 1 AND existing.IsDeleted = 0
);
PRINT 'keys declared: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' (expect 4 on a first run, 0 on a second)';
GO
INSERT INTO data.DataContainerKeyField (Id, DataContainerFieldId, DataContainerKeyRowId, KeyType, Ordinal, IsCurrent, IsDeleted)
SELECT NEWID(), f.Id, k.RowId, 'Surrogate', 0, 1, 0
FROM (VALUES
     (N'Teams',              N'PK_Scenario_Teams')
    ,(N'Players',            N'PK_Scenario_Players')
    ,(N'ScenarioValues',     N'PK_Scenario_ScenarioValues')
    ,(N'ScenarioDataOption', N'PK_Scenario_ScenarioDataOption')
) v(ContainerName, KeyName)
JOIN data.DataStore s ON s.Name = N'$(DataStore)' AND s.IsCurrent = 1 AND s.IsDeleted = 0
JOIN data.DataPath p ON p.DataStoreConfigurationRowId = (SELECT RowId FROM data.DataStoreConfiguration WHERE DataStoreRowId = s.RowId AND IsCurrent = 1 AND IsDeleted = 0)
                    AND p.Name = N'$(Path)' AND p.IsCurrent = 1 AND p.IsDeleted = 0
JOIN data.DataContainer c ON c.DataPathRowId = (SELECT RowId FROM data.DataPath WHERE Id = p.Id AND IsCurrent = 1 AND IsDeleted = 0) AND c.Name = v.ContainerName AND c.IsCurrent = 1 AND c.IsDeleted = 0
JOIN data.DataContainerKey k ON k.DataContainerRowId = c.RowId AND k.Name = v.KeyName AND k.IsCurrent = 1 AND k.IsDeleted = 0
JOIN data.DataContainerField f ON f.DataContainerRowId = c.RowId AND f.Name = N'Id' AND f.IsCurrent = 1 AND f.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM data.DataContainerKeyField existing
    WHERE existing.DataContainerFieldId = f.Id AND existing.DataContainerKeyRowId = k.RowId AND existing.KeyType = 'Surrogate' AND existing.IsCurrent = 1 AND existing.IsDeleted = 0
);
PRINT 'key fields declared: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' (expect 4 on a first run, 0 on a second)';
GO
-- Stop, as seed 38 does, rather than leave a container the routes will refuse: every one of the four must have a key with a key field.
IF EXISTS (
    SELECT 1
    FROM (VALUES (N'Teams'), (N'Players'), (N'ScenarioValues'), (N'ScenarioDataOption')) v(ContainerName)
    JOIN data.DataStore s ON s.Name = N'$(DataStore)' AND s.IsCurrent = 1 AND s.IsDeleted = 0
    JOIN data.DataPath p ON p.DataStoreConfigurationRowId = (SELECT RowId FROM data.DataStoreConfiguration WHERE DataStoreRowId = s.RowId AND IsCurrent = 1 AND IsDeleted = 0) AND p.Name = N'$(Path)' AND p.IsCurrent = 1 AND p.IsDeleted = 0
    JOIN data.DataContainer c ON c.DataPathRowId = p.RowId AND c.Name = v.ContainerName AND c.IsCurrent = 1 AND c.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM data.DataContainerKey k JOIN data.DataContainerKeyField kf ON kf.DataContainerKeyRowId = k.RowId AND kf.IsCurrent = 1 AND kf.IsDeleted = 0
                      WHERE k.DataContainerRowId = c.RowId AND k.IsCurrent = 1 AND k.IsDeleted = 0))
    THROW 50002, N'a fixture container has no key: the routes that page would refuse it', 1;
GO
-- 1. What the three DataOptions settings hold now. KEEP THESE LINES for the teardown. '(none)' means the setting did not exist.
SELECT N'OLD ' + v.SettingName + N'=' + COALESCE(i.SettingValue, N'(none)') AS KeepForTeardown
FROM (VALUES (N'DataOptionsDataStore'), (N'DataOptionsPath'), (N'DataOptionsContainer')) v(SettingName)
LEFT JOIN settings.ServerSetting d ON d.[Name] = v.SettingName AND d.IsCurrent = 1 AND d.IsDeleted = 0
LEFT JOIN settings.ServerSettingConfiguration i ON i.ServerSettingRowId = d.RowId AND i.IsCurrent = 1 AND i.IsDeleted = 0
ORDER BY v.SettingName;
GO
-- 2. Add the settings that do not exist (the shape of the route owner's settings.sql).
INSERT INTO settings.ServerSetting (Id, [Name], Domain, Implementation)
SELECT NEWID(), v.SettingName, 'ServerSetting', 'ServerSetting'
FROM (VALUES ('DataOptionsDataStore'), ('DataOptionsPath'), ('DataOptionsContainer')) v(SettingName)
WHERE NOT EXISTS (SELECT 1 FROM settings.ServerSetting x WHERE x.[Name] = v.SettingName AND x.IsCurrent = 1 AND x.IsDeleted = 0);

INSERT INTO settings.ServerSettingConfiguration (ServerSettingRowId, SettingValue, DataType, Description, IsActive)
SELECT d.RowId, v.SettingValue, 'String', v.Description, 1
FROM (VALUES
     ('DataOptionsDataStore', '$(DataStore)',        'The data store holding the options table (FDW-6365 scenario fixture).')
    ,('DataOptionsPath',      '$(Path)',             'The path of the options table (FDW-6365 scenario fixture).')
    ,('DataOptionsContainer', 'ScenarioDataOption',  'The container that is the options table (FDW-6365 scenario fixture).')
) v(SettingName, SettingValue, Description)
JOIN settings.ServerSetting d ON d.[Name] = v.SettingName AND d.IsCurrent = 1 AND d.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM settings.ServerSettingConfiguration i WHERE i.ServerSettingRowId = d.RowId AND i.IsCurrent = 1 AND i.IsDeleted = 0);
PRINT 'settings added: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
-- 3. A setting that already named another table is changed to the fixture's (the old value is in the lines printed in step 1).
UPDATE i SET SettingValue = v.SettingValue
FROM settings.ServerSettingConfiguration i
JOIN settings.ServerSetting d ON d.RowId = i.ServerSettingRowId AND d.IsCurrent = 1 AND d.IsDeleted = 0
JOIN (VALUES
     ('DataOptionsDataStore', '$(DataStore)')
    ,('DataOptionsPath',      '$(Path)')
    ,('DataOptionsContainer', 'ScenarioDataOption')
) v(SettingName, SettingValue) ON v.SettingName = d.[Name]
WHERE i.IsCurrent = 1 AND i.IsDeleted = 0 AND i.SettingValue <> v.SettingValue;
PRINT 'settings changed to the fixture''s values: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
-- 4. What the three settings hold now. Restart the host.
SELECT d.[Name], i.SettingValue
FROM settings.ServerSetting d
JOIN settings.ServerSettingConfiguration i ON i.ServerSettingRowId = d.RowId AND i.IsCurrent = 1 AND i.IsDeleted = 0
WHERE d.[Name] IN ('DataOptionsDataStore', 'DataOptionsPath', 'DataOptionsContainer') AND d.IsCurrent = 1 AND d.IsDeleted = 0
ORDER BY d.[Name];
GO
