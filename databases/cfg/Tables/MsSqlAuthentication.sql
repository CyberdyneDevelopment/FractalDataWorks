CREATE TABLE [cfg].[MsSqlAuthentication]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [ConfigurationId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Value] NVARCHAR(500) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlAuthentication_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_MsSqlAuthentication] PRIMARY KEY ([RowId])
    -- No FK constraint: ConfigurationId references MsSqlConnection.ConnectionId which isn't unique (versioning)
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_MsSqlAuthentication_Id_Current] ON [cfg].[MsSqlAuthentication]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per ConfigurationId+Name combination
CREATE UNIQUE INDEX [UX_MsSqlAuthentication_ConfigurationId_Name_Current] ON [cfg].[MsSqlAuthentication]([ConfigurationId], [Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_MsSqlAuthentication_Id] ON [cfg].[MsSqlAuthentication]([Id]);
GO

CREATE INDEX [IX_MsSqlAuthentication_ConfigurationId] ON [cfg].[MsSqlAuthentication]([ConfigurationId]);
GO

CREATE INDEX [IX_MsSqlAuthentication_IsCurrent] ON [cfg].[MsSqlAuthentication]([IsCurrent]);
GO

CREATE INDEX [IX_MsSqlAuthentication_IsDeleted] ON [cfg].[MsSqlAuthentication]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Key-value authentication settings for SQL Server connections with row-level versioning.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlAuthentication';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the authentication setting (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to MsSqlConnection.ConnectionId (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'ConfigurationId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the authentication setting (e.g., UserId, Password).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Value of the authentication setting.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'Value';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlAuthentication', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
