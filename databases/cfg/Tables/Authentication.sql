CREATE TABLE [cfg].[Authentication]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_Authentication_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [AuthenticationType] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    -- Pattern C Audit Columns
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_Authentication_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_Authentication_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_Authentication_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Authentication_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Authentication_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_Authentication_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Authentication_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_Authentication_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_Authentication] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_Authentication_Id_Current] ON [cfg].[Authentication]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per Name
CREATE UNIQUE INDEX [UX_Authentication_Name_Current] ON [cfg].[Authentication]([Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_Authentication_Id] ON [cfg].[Authentication]([Id]);
GO

CREATE INDEX [IX_Authentication_Name] ON [cfg].[Authentication]([Name]);
GO

CREATE INDEX [IX_Authentication_IsCurrent] ON [cfg].[Authentication]([IsCurrent]);
GO

CREATE INDEX [IX_Authentication_IsDeleted] ON [cfg].[Authentication]([IsDeleted]);
GO

-- Table description
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Base authentication configurations with row-level versioning. Parent table for authentication types.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'Authentication';
GO

-- Column descriptions
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Logical identity of the authentication config (persists across versions).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Unique name for the authentication configuration.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Type of authentication (ApiKey, Basic, OAuth2, Jwt, etc.).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'AuthenticationType';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional description of the authentication configuration.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Whether this is the current version of the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Soft delete flag.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Source system creation timestamp.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'When the record was created.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database principal who created the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'When the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database principal who last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'Authentication',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
