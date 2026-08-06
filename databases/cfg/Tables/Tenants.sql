CREATE TABLE [cfg].[Tenants]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_Tenants_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Slug] NVARCHAR(50) NOT NULL,
    [DisplayName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [ConnectionStringKey] NVARCHAR(100) NULL,
    [ConfigurationSection] NVARCHAR(100) NULL,
    [PrimaryColor] NVARCHAR(20) NULL,
    [SecondaryColor] NVARCHAR(20) NULL,
    [LogoUrl] NVARCHAR(500) NULL,
    [FaviconUrl] NVARCHAR(500) NULL,
    [CustomCssUrl] NVARCHAR(500) NULL,
    [DarkModeDefault] BIT NOT NULL DEFAULT 0,
    [MaxUsers] INT NULL,
    [StorageQuotaBytes] BIGINT NULL,
    [ApiRateLimitPerMinute] INT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_Tenants_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_Tenants_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_Tenants_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Tenants_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Tenants_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_Tenants_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Tenants_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Tenants_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_Tenants_Id_Current] ON [cfg].[Tenants]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per slug
CREATE UNIQUE INDEX [UX_Tenants_Slug_Current] ON [cfg].[Tenants]([Slug]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_Tenants_Id] ON [cfg].[Tenants]([Id]);
GO

CREATE INDEX [IX_Tenants_Slug] ON [cfg].[Tenants]([Slug]);
GO

CREATE INDEX [IX_Tenants_IsActive] ON [cfg].[Tenants]([IsActive]);
GO

CREATE INDEX [IX_Tenants_IsCurrent] ON [cfg].[Tenants]([IsCurrent]);
GO

CREATE INDEX [IX_Tenants_IsDeleted] ON [cfg].[Tenants]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Tenant configurations with row-level versioning. Multi-tenant settings including branding and limits.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'Tenants';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the tenant (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'URL-friendly unique identifier (e.g., acme-corp).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'Slug';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Human-readable display name for the tenant.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'DisplayName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether the tenant is active and can access the system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'IsActive';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Key to retrieve tenant-specific connection string.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'ConnectionStringKey';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Configuration section name for tenant-specific settings.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'ConfigurationSection';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Primary brand color for UI theming.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'PrimaryColor';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Secondary brand color for UI theming.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'SecondaryColor';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'URL to tenant-specific logo image.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'LogoUrl';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'URL to tenant-specific favicon.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'FaviconUrl';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'URL to tenant-specific custom CSS stylesheet.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'CustomCssUrl';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Default dark mode preference for this tenant UI.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'DarkModeDefault';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Maximum number of users allowed for this tenant.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'MaxUsers';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Storage quota in bytes for this tenant.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'StorageQuotaBytes';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'API rate limit in requests per minute.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'ApiRateLimitPerMinute';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'UTC timestamp when the tenant was originally created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'CreatedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'Tenants', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
