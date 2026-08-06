-- Migration: Add key definition columns to cfg.DataSet
-- Date: 2026-01-24
-- Purpose: Support surrogate and natural key definitions for unified schema system

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataSet]')
    AND [name] = 'SurrogateKeyName'
)
BEGIN
    ALTER TABLE [cfg].[DataSet]
    ADD [SurrogateKeyName] NVARCHAR(256) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'Name of the surrogate key definition (e.g., PK_DataSetName).',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataSet',
        @level2type=N'COLUMN', @level2name=N'SurrogateKeyName';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataSet]')
    AND [name] = 'SurrogateKeyFields'
)
BEGIN
    ALTER TABLE [cfg].[DataSet]
    ADD [SurrogateKeyFields] NVARCHAR(MAX) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'JSON array of field names forming the surrogate (auto-generated) key.',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataSet',
        @level2type=N'COLUMN', @level2name=N'SurrogateKeyFields';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataSet]')
    AND [name] = 'NaturalKeyName'
)
BEGIN
    ALTER TABLE [cfg].[DataSet]
    ADD [NaturalKeyName] NVARCHAR(256) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'Name of the natural key definition (e.g., AK_DataSetName).',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataSet',
        @level2type=N'COLUMN', @level2name=N'NaturalKeyName';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataSet]')
    AND [name] = 'NaturalKeyFields'
)
BEGIN
    ALTER TABLE [cfg].[DataSet]
    ADD [NaturalKeyFields] NVARCHAR(MAX) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'JSON array of field names forming the natural (business) key.',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataSet',
        @level2type=N'COLUMN', @level2name=N'NaturalKeyFields';
END
GO

PRINT 'Migration 002: Key columns added to cfg.DataSet';
GO
