CREATE TABLE [cfg].[OAuth2Authentication]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_RowId] DEFAULT NEWID(),
    [AuthenticationId] UNIQUEIDENTIFIER NOT NULL,
    [ClientId] NVARCHAR(256) NULL,
    [ClientSecret] NVARCHAR(1000) NULL,
    [SecretManagerName] NVARCHAR(256) NULL,
    [SecretKeyName] NVARCHAR(256) NULL,
    [TokenUrl] NVARCHAR(500) NULL,
    [Scope] NVARCHAR(500) NULL,
    [GrantType] NVARCHAR(100) NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_GrantType] DEFAULT ('client_credentials'),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OAuth2Authentication_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_OAuth2Authentication] PRIMARY KEY ([RowId])
    -- No FK constraint: AuthenticationId references Authentication.Id which isn't unique (versioning)
);
GO

-- One current version per parent AuthenticationId
CREATE UNIQUE INDEX [UX_OAuth2Authentication_AuthenticationId_Current] ON [cfg].[OAuth2Authentication]([AuthenticationId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_OAuth2Authentication_AuthenticationId] ON [cfg].[OAuth2Authentication]([AuthenticationId]);
GO

CREATE INDEX [IX_OAuth2Authentication_IsCurrent] ON [cfg].[OAuth2Authentication]([IsCurrent]);
GO

CREATE INDEX [IX_OAuth2Authentication_IsDeleted] ON [cfg].[OAuth2Authentication]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'OAuth 2.0 authentication configuration with row-level versioning. Child of cfg.Authentication.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'OAuth2Authentication';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Authentication.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'AuthenticationId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth 2.0 client identifier.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'ClientId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth 2.0 client secret.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'ClientSecret';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the secret manager for client secret.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'SecretManagerName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Secret key name within the secret manager.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'SecretKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth 2.0 token endpoint URL.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'TokenUrl';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth 2.0 scope parameter.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'Scope';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth 2.0 grant type.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'GrantType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OAuth2Authentication', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
