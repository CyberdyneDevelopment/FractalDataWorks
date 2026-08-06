CREATE TABLE [cfg].[OdbcConnection]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_RowId] DEFAULT NEWID(),
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [Driver] NVARCHAR(500) NULL,
    [Server] NVARCHAR(500) NULL,
    [Database] NVARCHAR(500) NULL,
    [Port] INT NULL,
    [CommandTimeoutSeconds] INT NULL,
    [ConnectionTimeoutSeconds] INT NULL,
    [ConnectionString] NVARCHAR(MAX) NULL,
    [AdditionalProperties] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_OdbcConnection_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_OdbcConnection] PRIMARY KEY ([RowId])
    -- No FK constraint: ConnectionId references Connection.Id which isn't unique (versioning)
);
GO

-- One current version per parent ConnectionId
CREATE UNIQUE INDEX [UX_OdbcConnection_ConnectionId_Current] ON [cfg].[OdbcConnection]([ConnectionId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_OdbcConnection_ConnectionId] ON [cfg].[OdbcConnection]([ConnectionId]);
GO

CREATE INDEX [IX_OdbcConnection_IsCurrent] ON [cfg].[OdbcConnection]([IsCurrent]);
GO

CREATE INDEX [IX_OdbcConnection_IsDeleted] ON [cfg].[OdbcConnection]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'ODBC connection configuration with row-level versioning. Child of cfg.Connection.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'OdbcConnection';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Connection.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'ConnectionId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ODBC driver name.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'Driver';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database server hostname or IP address.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'Server';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Target database name.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'Database';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'TCP port for the database server.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'Port';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Command execution timeout in seconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'CommandTimeoutSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Connection establishment timeout in seconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'ConnectionTimeoutSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Complete ODBC connection string (alternative to individual properties).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'ConnectionString';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON-serialized additional connection parameters.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'AdditionalProperties';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'OdbcConnection', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
