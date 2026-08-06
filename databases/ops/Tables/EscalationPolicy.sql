CREATE TABLE [ops].[EscalationPolicy]
(
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_Id] DEFAULT NEWID(),
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [IsEnabled] BIT NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_IsEnabled] DEFAULT (1),
    [ItemType] NVARCHAR(50) NULL,
    [WorkflowId] UNIQUEIDENTIFIER NULL,
    [ScheduleId] UNIQUEIDENTIFIER NULL,
    [MaxEscalationLevel] INT NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_MaxEscalationLevel] DEFAULT (3),
    [CooldownMinutes] INT NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_CooldownMinutes] DEFAULT (60),
    [ConditionExpression] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationPolicy_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_EscalationPolicy] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_EscalationPolicy_Name] UNIQUE ([Name])
);
GO

CREATE INDEX [IX_EscalationPolicy_Name] ON [ops].[EscalationPolicy]([Name]);
GO

CREATE INDEX [IX_EscalationPolicy_IsEnabled] ON [ops].[EscalationPolicy]([IsEnabled]);
GO

CREATE INDEX [IX_EscalationPolicy_ItemType] ON [ops].[EscalationPolicy]([ItemType]);
GO

CREATE INDEX [IX_EscalationPolicy_WorkflowId] ON [ops].[EscalationPolicy]([WorkflowId]);
GO

CREATE INDEX [IX_EscalationPolicy_ScheduleId] ON [ops].[EscalationPolicy]([ScheduleId]);
GO

CREATE INDEX [IX_EscalationPolicy_IsCurrent] ON [ops].[EscalationPolicy]([IsCurrent]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Escalation policy configuration. Defines when and how to notify stakeholders about execution failures and anomalies.',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Unique identifier for the escalation policy',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Unique name for the escalation policy',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Human-readable description of the escalation policy purpose',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Whether this policy is currently active',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'IsEnabled';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Type of execution item this policy applies to (Workflow, Job, Task, etc.)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'ItemType';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Specific workflow this policy applies to (null for all workflows)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'WorkflowId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Specific schedule this policy applies to (null for all schedules)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'ScheduleId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Maximum escalation level before stopping notifications',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'MaxEscalationLevel';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Cooldown period in minutes between escalation attempts',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'CooldownMinutes';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Filter expression to determine when this policy applies',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'ConditionExpression';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this is the current version of the record',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this record has been soft deleted',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Original creation date from source system (if migrated)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was created',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that created this record',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was created',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that last modified this record',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationPolicy',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
