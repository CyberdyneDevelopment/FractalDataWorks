CREATE TABLE [cfg].[BasicAuthentication]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_RowId] DEFAULT NEWID(),
    [AuthenticationId] UNIQUEIDENTIFIER NOT NULL,
    [Username] NVARCHAR(500) NULL,
    [Password] NVARCHAR(1000) NULL,
    [SecretManagerName] NVARCHAR(256) NULL,
    [SecretKeyName] NVARCHAR(256) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_BasicAuthentication_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_BasicAuthentication] PRIMARY KEY ([RowId])
    -- No FK constraint: AuthenticationId references Authentication.Id which isn't unique (versioning)
);
GO

-- One current version per parent AuthenticationId
CREATE UNIQUE INDEX [UX_BasicAuthentication_AuthenticationId_Current] ON [cfg].[BasicAuthentication]([AuthenticationId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_BasicAuthentication_AuthenticationId] ON [cfg].[BasicAuthentication]([AuthenticationId]);
GO

CREATE INDEX [IX_BasicAuthentication_IsCurrent] ON [cfg].[BasicAuthentication]([IsCurrent]);
GO

CREATE INDEX [IX_BasicAuthentication_IsDeleted] ON [cfg].[BasicAuthentication]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Basic authentication configuration with row-level versioning. Child of cfg.Authentication.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'BasicAuthentication';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Authentication.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'AuthenticationId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Username for basic authentication.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'Username';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Password. Mutually exclusive with SecretManagerName.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'Password';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the secret manager to retrieve credentials.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'SecretManagerName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Secret key name within the secret manager.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'SecretKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'BasicAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
