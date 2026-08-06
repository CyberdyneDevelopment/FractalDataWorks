CREATE TABLE [cfg].[ConfigurationAudit]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] NVARCHAR(200) NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,
    [BeforeJson] NVARCHAR(MAX) NULL,
    [AfterJson] NVARCHAR(MAX) NULL,
    [ChangedFields] NVARCHAR(MAX) NULL,
    [UserId] NVARCHAR(100) NOT NULL,
    [UserName] NVARCHAR(200) NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [CorrelationId] UNIQUEIDENTIFIER NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ConfigurationAudit_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_ConfigurationAudit] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id (audit records are typically immutable, but pattern consistency)
CREATE UNIQUE INDEX [UX_ConfigurationAudit_Id_Current] ON [cfg].[ConfigurationAudit]([Id]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_ConfigurationAudit_Id] ON [cfg].[ConfigurationAudit]([Id]);
GO

CREATE INDEX [IX_ConfigurationAudit_EntityType] ON [cfg].[ConfigurationAudit]([EntityType]);
GO

CREATE INDEX [IX_ConfigurationAudit_EntityId] ON [cfg].[ConfigurationAudit]([EntityId]);
GO

CREATE INDEX [IX_ConfigurationAudit_Action] ON [cfg].[ConfigurationAudit]([Action]);
GO

CREATE INDEX [IX_ConfigurationAudit_UserId] ON [cfg].[ConfigurationAudit]([UserId]);
GO

CREATE INDEX [IX_ConfigurationAudit_Timestamp] ON [cfg].[ConfigurationAudit]([Timestamp] DESC);
GO

CREATE INDEX [IX_ConfigurationAudit_CorrelationId] ON [cfg].[ConfigurationAudit]([CorrelationId]) WHERE [CorrelationId] IS NOT NULL;
GO

CREATE INDEX [IX_ConfigurationAudit_IsCurrent] ON [cfg].[ConfigurationAudit]([IsCurrent]);
GO

CREATE INDEX [IX_ConfigurationAudit_IsDeleted] ON [cfg].[ConfigurationAudit]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Audit trail for all configuration changes with row-level versioning. Tracks create, update, delete operations.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'ConfigurationAudit';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the audit record (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Type of entity that was modified (e.g., Connection, DataSet, Tenant).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'EntityType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Identifier of the specific entity instance that was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'EntityId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Action performed on the entity (e.g., Create, Update, Delete).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'Action';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON snapshot of the entity before the change (null for Create actions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'BeforeJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON snapshot of the entity after the change (null for Delete actions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'AfterJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of field names that changed during the update.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'ChangedFields';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Identifier of the user who performed the action.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'UserId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Display name of the user who performed the action.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'UserName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'IP address from which the action was performed.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'IpAddress';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User agent string from the client that performed the action.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'UserAgent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'UTC timestamp when the action was performed.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'Timestamp';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Correlation identifier for tracking related operations.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'CorrelationId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ConfigurationAudit', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
