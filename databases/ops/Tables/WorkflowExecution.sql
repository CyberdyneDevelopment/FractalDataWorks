CREATE TABLE [ops].[WorkflowExecution]
(
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_Id] DEFAULT NEWID(),
    [WorkflowName] NVARCHAR(200) NOT NULL,
    [CorrelationId] UNIQUEIDENTIFIER NOT NULL,
    [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_Status] DEFAULT ('Pending'),
    [StartedAt] DATETIMEOFFSET NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_StartedAt] DEFAULT (sysdatetimeoffset()),
    [CompletedAt] DATETIMEOFFSET NULL,
    [DurationMs] BIGINT NULL,
    [TotalSteps] INT NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_TotalSteps] DEFAULT (0),
    [CompletedSteps] INT NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_CompletedSteps] DEFAULT (0),
    [FailedSteps] INT NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_FailedSteps] DEFAULT (0),
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [ExecutedBy] NVARCHAR(200) NULL,
    [TenantId] UNIQUEIDENTIFIER NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_WorkflowExecution_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_WorkflowExecution] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_WorkflowExecution_Status] CHECK ([Status] IN ('Pending', 'Running', 'Completed', 'Failed', 'Cancelled'))
);
GO

CREATE INDEX [IX_WorkflowExecution_CorrelationId] ON [ops].[WorkflowExecution]([CorrelationId]);
GO

CREATE INDEX [IX_WorkflowExecution_Status] ON [ops].[WorkflowExecution]([Status]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Tracks workflow execution instances', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Unique identifier for the workflow execution', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'Id';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Name of the workflow being executed', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'WorkflowName';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Correlation ID for tracking related operations across distributed systems', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'CorrelationId';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Current status: Pending, Running, Completed, Failed, Cancelled', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'Status';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Timestamp when the workflow execution started', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'StartedAt';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Timestamp when the workflow execution completed (null if still running)', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'CompletedAt';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Total execution duration in milliseconds', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'DurationMs';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Total number of steps in the workflow', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'TotalSteps';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Number of steps completed successfully', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'CompletedSteps';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Number of steps that failed', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'FailedSteps';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Error message if workflow failed', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'ErrorMessage';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'User or service that initiated the workflow execution', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'ExecutedBy';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Tenant isolation identifier for multi-tenant scenarios', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'TenantId';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Flag indicating whether this is the current version of the record', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'IsCurrent';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Flag indicating whether this record has been soft deleted', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'IsDeleted';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Original creation date from source system (if migrated)', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'SrcCreateDate';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Timestamp when this record was created', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'CreateDate';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'User or service account that created this record', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'CreateBy';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'User on whose behalf this record was created', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'CreateOnBehalfOf';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Timestamp when this record was last modified', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'ModifyDate';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'User or service account that last modified this record', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'ModifyBy';
GO

EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'User on whose behalf this record was last modified', @level0type = N'SCHEMA', @level0name = N'ops', @level1type = N'TABLE', @level1name = N'WorkflowExecution', @level2type = N'COLUMN', @level2name = N'ModifyOnBehalfOf';
GO
