-- Migration: Add key definition columns to cfg.DataContainer
-- Date: 2026-01-24
-- Purpose: Support surrogate and natural key definitions for unified schema system

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataContainer]')
    AND [name] = 'SurrogateKeyName'
)
BEGIN
    ALTER TABLE [cfg].[DataContainer]
    ADD [SurrogateKeyName] NVARCHAR(256) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'Name of the surrogate key constraint (e.g., PK_TableName).',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataContainer',
        @level2type=N'COLUMN', @level2name=N'SurrogateKeyName';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataContainer]')
    AND [name] = 'SurrogateKeyColumns'
)
BEGIN
    ALTER TABLE [cfg].[DataContainer]
    ADD [SurrogateKeyColumns] NVARCHAR(MAX) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'JSON array of column names forming the surrogate (auto-generated) key.',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataContainer',
        @level2type=N'COLUMN', @level2name=N'SurrogateKeyColumns';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataContainer]')
    AND [name] = 'NaturalKeyName'
)
BEGIN
    ALTER TABLE [cfg].[DataContainer]
    ADD [NaturalKeyName] NVARCHAR(256) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'Name of the natural key constraint (e.g., AK_TableName).',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataContainer',
        @level2type=N'COLUMN', @level2name=N'NaturalKeyName';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID('[cfg].[DataContainer]')
    AND [name] = 'NaturalKeyColumns'
)
BEGIN
    ALTER TABLE [cfg].[DataContainer]
    ADD [NaturalKeyColumns] NVARCHAR(MAX) NULL;

    EXEC sys.sp_addextendedproperty
        @name=N'MS_Description',
        @value=N'JSON array of column names forming the natural (business) key.',
        @level0type=N'SCHEMA', @level0name=N'cfg',
        @level1type=N'TABLE', @level1name=N'DataContainer',
        @level2type=N'COLUMN', @level2name=N'NaturalKeyColumns';
END
GO

PRINT 'Migration 001: Key columns added to cfg.DataContainer';
GO
