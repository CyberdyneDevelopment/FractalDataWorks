CREATE TABLE [cfg].[EnvironmentVariableSecretManager]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_RowId] DEFAULT NEWID(),
    [SecretManagerId] UNIQUEIDENTIFIER NOT NULL,
    [Prefix] NVARCHAR(100) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_Prefix] DEFAULT 'FDW_SECRET_',
    [CaseSensitive] BIT NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_CaseSensitive] DEFAULT (0),
    [Separator] NVARCHAR(10) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_Separator] DEFAULT '__',
    [StripPrefix] BIT NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_StripPrefix] DEFAULT (1),
    [Target] NVARCHAR(50) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_Target] DEFAULT 'Process',
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EnvironmentVariableSecretManager_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_EnvironmentVariableSecretManager] PRIMARY KEY ([RowId])
    -- No FK constraint: SecretManagerId references SecretManager.Id which isn't unique (versioning)
);
GO

-- One current version per parent SecretManagerId
CREATE UNIQUE INDEX [UX_EnvironmentVariableSecretManager_SecretManagerId_Current] ON [cfg].[EnvironmentVariableSecretManager]([SecretManagerId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_EnvironmentVariableSecretManager_SecretManagerId] ON [cfg].[EnvironmentVariableSecretManager]([SecretManagerId]);
GO

CREATE INDEX [IX_EnvironmentVariableSecretManager_IsCurrent] ON [cfg].[EnvironmentVariableSecretManager]([IsCurrent]);
GO

CREATE INDEX [IX_EnvironmentVariableSecretManager_IsDeleted] ON [cfg].[EnvironmentVariableSecretManager]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Environment variable secret manager with row-level versioning. Child of cfg.SecretManager.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'EnvironmentVariableSecretManager';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to SecretManager.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'SecretManagerId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Prefix for secret environment variables.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'Prefix';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether matching is case-sensitive.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'CaseSensitive';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Separator for hierarchical keys.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'Separator';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether to strip prefix from key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'StripPrefix';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Environment variable target (Process, User, Machine).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'Target';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EnvironmentVariableSecretManager', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
