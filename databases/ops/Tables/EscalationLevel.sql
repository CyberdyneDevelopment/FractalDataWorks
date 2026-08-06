CREATE TABLE [ops].[EscalationLevel]
(
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ops_EscalationLevel_Id] DEFAULT NEWID(),
    [EscalationPolicyId] UNIQUEIDENTIFIER NOT NULL,
    [Level] INT NOT NULL,
    [DelayMinutes] INT NOT NULL CONSTRAINT [DF_ops_EscalationLevel_DelayMinutes] DEFAULT (0),
    [NotificationChannel] NVARCHAR(50) NOT NULL,
    [Recipients] NVARCHAR(MAX) NOT NULL,
    [Template] NVARCHAR(100) NULL,
    [Severity] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ops_EscalationLevel_Severity] DEFAULT ('Warning'),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_ops_EscalationLevel_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ops_EscalationLevel_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_EscalationLevel_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationLevel_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationLevel_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_EscalationLevel_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationLevel_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_EscalationLevel_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_EscalationLevel] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EscalationLevel_Policy] FOREIGN KEY ([EscalationPolicyId])
        REFERENCES [ops].[EscalationPolicy]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_EscalationLevel_PolicyLevel] UNIQUE ([EscalationPolicyId], [Level])
);
GO

CREATE INDEX [IX_EscalationLevel_PolicyId] ON [ops].[EscalationLevel]([EscalationPolicyId]);
GO

CREATE INDEX [IX_EscalationLevel_Level] ON [ops].[EscalationLevel]([Level]);
GO

CREATE INDEX [IX_EscalationLevel_NotificationChannel] ON [ops].[EscalationLevel]([NotificationChannel]);
GO

CREATE INDEX [IX_EscalationLevel_IsCurrent] ON [ops].[EscalationLevel]([IsCurrent]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Individual escalation levels within a policy. Defines when, who, how, and what severity for each escalation tier.',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Unique identifier for the escalation level',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Foreign key to the parent escalation policy',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'EscalationPolicyId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Level number (1 = first notification, 2 = second, etc.)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'Level';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Delay in minutes before triggering this escalation level',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'DelayMinutes';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Notification channel to use (Email, Slack, Teams, PagerDuty, etc.)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'NotificationChannel';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'JSON array or delimited list of recipient addresses/identifiers',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'Recipients';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Template name for formatting the notification message',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'Template';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Severity level for this escalation (Warning, Error, Critical)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'Severity';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this is the current version of the record',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this record has been soft deleted',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Original creation date from source system (if migrated)',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was created',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that created this record',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was created',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that last modified this record',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'ops',
    @level1type=N'TABLE',  @level1name=N'EscalationLevel',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
