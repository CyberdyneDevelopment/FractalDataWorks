CREATE TABLE [cfg].[MsSqlConnection]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_RowId] DEFAULT NEWID(),
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [Server] NVARCHAR(200) NOT NULL,
    [Database] NVARCHAR(200) NOT NULL,
    [UserId] NVARCHAR(200) NULL,
    [PasswordSecretRef] NVARCHAR(500) NULL,
    [UseIntegratedSecurity] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_UseIntegratedSecurity] DEFAULT (0),
    [ConnectionTimeout] INT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_ConnectionTimeout] DEFAULT (30),
    [CommandTimeout] INT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_CommandTimeout] DEFAULT (30),
    [TrustServerCertificate] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_TrustServerCertificate] DEFAULT (1),
    [Encrypt] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_Encrypt] DEFAULT (0),
    [MultipleActiveResultSets] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_MultipleActiveResultSets] DEFAULT (1),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlConnection_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_MsSqlConnection] PRIMARY KEY ([RowId])
    -- No FK constraint: ConnectionId references Connection.Id which isn't unique (versioning)
    -- Referential integrity enforced by application + IsCurrent=1 filter
);
GO

-- One current version per parent ConnectionId
CREATE UNIQUE INDEX [UX_MsSqlConnection_ConnectionId_Current] ON [cfg].[MsSqlConnection]([ConnectionId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_MsSqlConnection_ConnectionId] ON [cfg].[MsSqlConnection]([ConnectionId]);
GO

CREATE INDEX [IX_MsSqlConnection_IsCurrent] ON [cfg].[MsSqlConnection]([IsCurrent]);
GO

CREATE INDEX [IX_MsSqlConnection_IsDeleted] ON [cfg].[MsSqlConnection]([IsDeleted]);
GO

-- Table description
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Microsoft SQL Server connection configuration with row-level versioning. Child of cfg.Connection.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection';
GO

-- Column descriptions
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Version-specific row identifier (primary key).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Foreign key to parent Connection.Id (logical identity, join WHERE IsCurrent=1).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'ConnectionId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'SQL Server hostname or IP address (e.g., localhost, sql.example.com).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'Server';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Target database name on the SQL Server instance.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'Database';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'SQL Server login username (null when UseIntegratedSecurity is true).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'UserId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Reference to secret containing the password (retrieved via ISecretManagerProvider).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'PasswordSecretRef';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'When true, uses Windows Authentication instead of SQL Server authentication.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'UseIntegratedSecurity';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Connection timeout in seconds (default: 30).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'ConnectionTimeout';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Command execution timeout in seconds (default: 30).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'CommandTimeout';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'When true, trusts the server certificate without validation.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'TrustServerCertificate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'When true, encrypts the connection using TLS.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'Encrypt';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'When true, allows multiple active result sets on a single connection.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'MultipleActiveResultSets';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Indicates if this is the current version of the configuration.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Soft delete flag. True indicates the record is logically deleted.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Original creation timestamp from source system (if applicable).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when the record was created in this system.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Security principal (login) that created the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf the record was created (for delegation scenarios).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Security principal (login) that last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf the record was last modified (for delegation scenarios).',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlConnection',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
