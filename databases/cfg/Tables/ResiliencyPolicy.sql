CREATE TABLE [cfg].[ResiliencyPolicy]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] VARCHAR(256) NOT NULL,
    [Description] VARCHAR(500) NULL,
    [MaxRetries] INT NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_MaxRetries] DEFAULT (3),
    [RetryDelayMs] INT NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_RetryDelayMs] DEFAULT (1000),
    [BackoffMultiplier] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_BackoffMultiplier] DEFAULT (2.0),
    [MaxDelayMs] INT NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_MaxDelayMs] DEFAULT (30000),
    [TimeoutMs] INT NULL,
    [CircuitBreakerThreshold] INT NULL,
    [CircuitBreakerDurationMs] INT NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_ResiliencyPolicy_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_ResiliencyPolicy] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_ResiliencyPolicy_Id_Current] ON [cfg].[ResiliencyPolicy]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per name
CREATE UNIQUE INDEX [UX_ResiliencyPolicy_Name_Current] ON [cfg].[ResiliencyPolicy]([Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_ResiliencyPolicy_Id] ON [cfg].[ResiliencyPolicy]([Id]);
GO

CREATE INDEX [IX_ResiliencyPolicy_Name] ON [cfg].[ResiliencyPolicy]([Name]);
GO

CREATE INDEX [IX_ResiliencyPolicy_IsCurrent] ON [cfg].[ResiliencyPolicy]([IsCurrent]);
GO

CREATE INDEX [IX_ResiliencyPolicy_IsDeleted] ON [cfg].[ResiliencyPolicy]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Resiliency policy configurations with row-level versioning for retry, timeout, and circuit breaker patterns.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'ResiliencyPolicy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the policy (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique name of the resiliency policy.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Description of the resiliency policy.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Maximum retry attempts before giving up.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'MaxRetries';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Initial delay in milliseconds between retries.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'RetryDelayMs';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Exponential backoff multiplier.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'BackoffMultiplier';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Maximum delay in milliseconds (backoff cap).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'MaxDelayMs';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Operation timeout in milliseconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'TimeoutMs';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Failures before circuit breaker opens.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'CircuitBreakerThreshold';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Circuit breaker open duration in milliseconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'CircuitBreakerDurationMs';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'ResiliencyPolicy', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
