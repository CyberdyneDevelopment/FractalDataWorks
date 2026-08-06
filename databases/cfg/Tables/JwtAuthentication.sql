CREATE TABLE [cfg].[JwtAuthentication]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_RowId] DEFAULT NEWID(),
    [AuthenticationId] UNIQUEIDENTIFIER NOT NULL,
    [Issuer] NVARCHAR(500) NULL,
    [Audience] NVARCHAR(500) NULL,
    [SecretKey] NVARCHAR(1000) NULL,
    [SecretManagerName] NVARCHAR(256) NULL,
    [SecretKeyName] NVARCHAR(256) NULL,
    [ClientId] NVARCHAR(256) NULL,
    [Authority] NVARCHAR(500) NULL,
    [RedirectUri] NVARCHAR(500) NULL,
    [AccessTokenExpirationMinutes] INT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_AccessTokenExpirationMinutes] DEFAULT (60),
    [RefreshTokenExpirationMinutes] INT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_RefreshTokenExpirationMinutes] DEFAULT (10080),
    [TokenCacheLifetimeMinutes] INT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_TokenCacheLifetimeMinutes] DEFAULT (60),
    [EnableTokenCaching] BIT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_EnableTokenCaching] DEFAULT (1),
    [ValidateIssuer] BIT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ValidateIssuer] DEFAULT (1),
    [ValidateAudience] BIT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ValidateAudience] DEFAULT (1),
    [ValidateLifetime] BIT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ValidateLifetime] DEFAULT (1),
    [ValidateIssuerSigningKey] BIT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ValidateIssuerSigningKey] DEFAULT (1),
    [ClockSkewSeconds] INT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ClockSkewSeconds] DEFAULT (30),
    [RoleClaimType] NVARCHAR(100) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_RoleClaimType] DEFAULT ('role'),
    [NameClaimType] NVARCHAR(100) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_NameClaimType] DEFAULT ('name'),
    [UserStoreConnectionName] NVARCHAR(256) NULL,
    [UsersTableName] NVARCHAR(256) NULL CONSTRAINT [DF_cfg_JwtAuthentication_UsersTableName] DEFAULT ('Users'),
    [UserRolesTableName] NVARCHAR(256) NULL CONSTRAINT [DF_cfg_JwtAuthentication_UserRolesTableName] DEFAULT ('UserRoles'),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_JwtAuthentication_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_JwtAuthentication] PRIMARY KEY ([RowId])
    -- No FK constraint: AuthenticationId references Authentication.Id which isn't unique (versioning)
);
GO

-- One current version per parent AuthenticationId
CREATE UNIQUE INDEX [UX_JwtAuthentication_AuthenticationId_Current] ON [cfg].[JwtAuthentication]([AuthenticationId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_JwtAuthentication_AuthenticationId] ON [cfg].[JwtAuthentication]([AuthenticationId]);
GO

CREATE INDEX [IX_JwtAuthentication_IsCurrent] ON [cfg].[JwtAuthentication]([IsCurrent]);
GO

CREATE INDEX [IX_JwtAuthentication_IsDeleted] ON [cfg].[JwtAuthentication]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'JWT authentication configuration with row-level versioning. Child of cfg.Authentication.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'JwtAuthentication';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Authentication.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'AuthenticationId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JWT issuer claim (iss).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'Issuer';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JWT audience claim (aud).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'Audience';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Symmetric key for signing/validating tokens.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'SecretKey';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the secret manager for signing key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'SecretManagerName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Secret key name within the secret manager.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'SecretKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth/OIDC client identifier.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ClientId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth/OIDC authority URL.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'Authority';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'OAuth/OIDC redirect URI.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'RedirectUri';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Access token lifetime in minutes.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'AccessTokenExpirationMinutes';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Refresh token lifetime in minutes.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'RefreshTokenExpirationMinutes';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Token cache lifetime in minutes.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'TokenCacheLifetimeMinutes';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Enable in-memory token caching.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'EnableTokenCaching';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Validate the issuer claim.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ValidateIssuer';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Validate the audience claim.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ValidateAudience';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Validate token expiration.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ValidateLifetime';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Validate the signing key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ValidateIssuerSigningKey';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Clock skew allowance in seconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ClockSkewSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Claim type name for role claims.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'RoleClaimType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Claim type name for name/identity claims.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'NameClaimType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Connection name for user store database.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'UserStoreConnectionName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Table name for users in user store.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'UsersTableName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Table name for user-role mappings.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'UserRolesTableName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'JwtAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
