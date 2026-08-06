CREATE TABLE [cfg].[PostgreSqlConnection]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_RowId] DEFAULT NEWID(),
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [Server] NVARCHAR(500) NULL,
    [Database] NVARCHAR(500) NULL,
    [Port] INT NULL,
    [CommandTimeoutSeconds] INT NULL,
    [ConnectionTimeoutSeconds] INT NULL,
    [DefaultSchema] NVARCHAR(500) NULL,
    [SslMode] NVARCHAR(50) NULL,
    [TrustServerCertificate] BIT NULL,
    [EnableConnectionPooling] BIT NULL,
    [MinPoolSize] INT NULL,
    [MaxPoolSize] INT NULL,
    [ApplicationName] NVARCHAR(500) NULL,
    [AdditionalProperties] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_PostgreSqlConnection_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_PostgreSqlConnection] PRIMARY KEY ([RowId])
    -- No FK constraint: ConnectionId references Connection.Id which isn't unique (versioning)
);
GO

-- One current version per parent ConnectionId
CREATE UNIQUE INDEX [UX_PostgreSqlConnection_ConnectionId_Current] ON [cfg].[PostgreSqlConnection]([ConnectionId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_PostgreSqlConnection_ConnectionId] ON [cfg].[PostgreSqlConnection]([ConnectionId]);
GO

CREATE INDEX [IX_PostgreSqlConnection_IsCurrent] ON [cfg].[PostgreSqlConnection]([IsCurrent]);
GO

CREATE INDEX [IX_PostgreSqlConnection_IsDeleted] ON [cfg].[PostgreSqlConnection]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'PostgreSQL connection configuration with row-level versioning. Child of cfg.Connection.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'PostgreSqlConnection';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Connection.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'ConnectionId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'PostgreSQL server hostname or IP address.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'Server';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Target database name.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'Database';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'TCP port (default: 5432).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'Port';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Command execution timeout in seconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'CommandTimeoutSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Connection establishment timeout in seconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'ConnectionTimeoutSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Default schema for unqualified object references.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'DefaultSchema';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SSL/TLS mode (Require, Prefer, Disable).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'SslMode';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether to trust server certificate without validation.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'TrustServerCertificate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether connection pooling is enabled.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'EnableConnectionPooling';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Minimum connections in pool.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'MinPoolSize';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Maximum connections in pool.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'MaxPoolSize';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application name for PostgreSQL logs.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'ApplicationName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON-serialized additional connection parameters.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'AdditionalProperties';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'PostgreSqlConnection', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
