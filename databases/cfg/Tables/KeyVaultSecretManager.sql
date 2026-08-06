CREATE TABLE [cfg].[KeyVaultSecretManager]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_RowId] DEFAULT NEWID(),
    [SecretManagerId] UNIQUEIDENTIFIER NOT NULL,
    [VaultUri] NVARCHAR(500) NULL,
    [TenantId] NVARCHAR(256) NULL,
    [ClientId] NVARCHAR(256) NULL,
    [ClientSecret] NVARCHAR(500) NULL,
    [UseManagedIdentity] BIT NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_UseManagedIdentity] DEFAULT (0),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_KeyVaultSecretManager_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_KeyVaultSecretManager] PRIMARY KEY ([RowId])
    -- No FK constraint: SecretManagerId references SecretManager.Id which isn't unique (versioning)
);
GO

-- One current version per parent SecretManagerId
CREATE UNIQUE INDEX [UX_KeyVaultSecretManager_SecretManagerId_Current] ON [cfg].[KeyVaultSecretManager]([SecretManagerId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_KeyVaultSecretManager_SecretManagerId] ON [cfg].[KeyVaultSecretManager]([SecretManagerId]);
GO

CREATE INDEX [IX_KeyVaultSecretManager_IsCurrent] ON [cfg].[KeyVaultSecretManager]([IsCurrent]);
GO

CREATE INDEX [IX_KeyVaultSecretManager_IsDeleted] ON [cfg].[KeyVaultSecretManager]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Azure Key Vault secret manager with row-level versioning. Child of cfg.SecretManager.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'KeyVaultSecretManager';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to SecretManager.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'SecretManagerId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Azure Key Vault URI.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'VaultUri';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Azure AD tenant identifier.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'TenantId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Azure AD application client ID.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'ClientId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Azure AD application client secret.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'ClientSecret';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Use Azure Managed Identity for auth.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'UseManagedIdentity';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'KeyVaultSecretManager', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
