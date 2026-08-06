CREATE TABLE [cfg].[SecretManager]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_SecretManager_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [SecretManagerType] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_SecretManager_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_SecretManager_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_SecretManager_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_SecretManager_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_SecretManager_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_SecretManager_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_SecretManager_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_SecretManager_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_SecretManager] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_SecretManager_Id_Current] ON [cfg].[SecretManager]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per Name
CREATE UNIQUE INDEX [UX_SecretManager_Name_Current] ON [cfg].[SecretManager]([Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_SecretManager_Id] ON [cfg].[SecretManager]([Id]);
GO

CREATE INDEX [IX_SecretManager_Name] ON [cfg].[SecretManager]([Name]);
GO

CREATE INDEX [IX_SecretManager_SecretManagerType] ON [cfg].[SecretManager]([SecretManagerType]);
GO

CREATE INDEX [IX_SecretManager_IsCurrent] ON [cfg].[SecretManager]([IsCurrent]);
GO

CREATE INDEX [IX_SecretManager_IsDeleted] ON [cfg].[SecretManager]([IsDeleted]);
GO

-- Table description
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Base secret manager configurations with row-level versioning. Parent table for secret manager types.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager';
GO

-- Column descriptions
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Version-specific row identifier (primary key).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Logical identity of the secret manager (persists across versions).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Unique name for the secret manager.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Type discriminator (KeyVault, EnvironmentVariable, etc.).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'SecretManagerType';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Optional description of the secret manager purpose.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Whether this is the current version of the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Soft delete flag.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Original creation date from source system.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'When the record was created.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Database principal who created the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Application user on whose behalf the record was created.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'When the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Database principal who last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Application user on whose behalf the record was modified.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'SecretManager',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
