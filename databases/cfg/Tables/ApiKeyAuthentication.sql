CREATE TABLE [cfg].[ApiKeyAuthentication]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_RowId] DEFAULT NEWID(),
    [AuthenticationId] UNIQUEIDENTIFIER NOT NULL,
    [ApiKey] NVARCHAR(1000) NULL,
    [SecretManagerName] NVARCHAR(256) NULL,
    [SecretKeyName] NVARCHAR(256) NULL,
    [HeaderName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_HeaderName] DEFAULT ('X-API-Key'),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ApiKeyAuthentication_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_ApiKeyAuthentication] PRIMARY KEY ([RowId])
    -- No FK constraint: AuthenticationId references Authentication.Id which isn't unique (versioning)
);
GO

-- One current version per parent AuthenticationId
CREATE UNIQUE INDEX [UX_ApiKeyAuthentication_AuthenticationId_Current] ON [cfg].[ApiKeyAuthentication]([AuthenticationId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_ApiKeyAuthentication_AuthenticationId] ON [cfg].[ApiKeyAuthentication]([AuthenticationId]);
GO

CREATE INDEX [IX_ApiKeyAuthentication_IsCurrent] ON [cfg].[ApiKeyAuthentication]([IsCurrent]);
GO

CREATE INDEX [IX_ApiKeyAuthentication_IsDeleted] ON [cfg].[ApiKeyAuthentication]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'API Key authentication configuration with row-level versioning. Child of cfg.Authentication.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'ApiKeyAuthentication';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Authentication.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'AuthenticationId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'The API key value. Mutually exclusive with SecretManagerName.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'ApiKey';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the secret manager to retrieve the API key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'SecretManagerName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Secret key name within the secret manager.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'SecretKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'HTTP header name containing the API key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'HeaderName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ApiKeyAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
