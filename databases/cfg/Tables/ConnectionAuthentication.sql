CREATE TABLE [cfg].[ConnectionAuthentication]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_RowId] DEFAULT NEWID(),
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [Type] NVARCHAR(50) NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_Type] DEFAULT ('SqlAuth'),
    [Username] NVARCHAR(200) NULL,
    [SecretManagerName] NVARCHAR(256) NULL,
    [SecretKeyName] NVARCHAR(256) NULL,
    -- Pattern C Audit Columns
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConnectionAuthentication_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_ConnectionAuthentication] PRIMARY KEY ([RowId])
    -- No FK constraint: ConnectionId references Connection.Id which isn't unique (versioning)
    -- Referential integrity enforced by application + IsCurrent=1 filter
);
GO

-- One current version per parent ConnectionId
CREATE UNIQUE INDEX [UX_ConnectionAuthentication_ConnectionId_Current] ON [cfg].[ConnectionAuthentication]([ConnectionId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_ConnectionAuthentication_ConnectionId] ON [cfg].[ConnectionAuthentication]([ConnectionId]);
GO

CREATE INDEX [IX_ConnectionAuthentication_Type] ON [cfg].[ConnectionAuthentication]([Type]);
GO

CREATE INDEX [IX_ConnectionAuthentication_IsCurrent] ON [cfg].[ConnectionAuthentication]([IsCurrent]);
GO

CREATE INDEX [IX_ConnectionAuthentication_IsDeleted] ON [cfg].[ConnectionAuthentication]([IsDeleted]);
GO

-- Table description
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Authentication configuration for connections with row-level versioning. Child of cfg.Connection.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'ConnectionAuthentication';
GO

-- Column descriptions
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Foreign key to parent Connection.Id (logical identity, join WHERE IsCurrent=1).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'ConnectionId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Authentication type (SqlAuth, WindowsAuth, etc.). Defaults to SqlAuth.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'Type';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Username for authentication. Can be stored directly or retrieved from secret manager.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'Username';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Name of the secret manager to retrieve credentials from.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'SecretManagerName';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Secret key name within the secret manager for credential retrieval.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'SecretKeyName';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates if this is the current version of the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates if this record has been soft-deleted.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Source system creation timestamp (if migrated from another system).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Timestamp when the record was created in this system.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'User who created the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'User on whose behalf the record was created (for delegation scenarios).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Timestamp when the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'User who last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'User on whose behalf the record was modified (for delegation scenarios).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'ConnectionAuthentication',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
