CREATE TABLE [sched].[Schedule]
(
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    [ServiceOptionType] NVARCHAR(100) NOT NULL DEFAULT 'Cron',
    [PipelineName] NVARCHAR(200) NULL,
    [CronExpression] NVARCHAR(100) NULL,
    [IntervalSeconds] INT NULL,
    [TimeZoneId] NVARCHAR(100) NOT NULL DEFAULT 'UTC',
    [IsEnabled] BIT NOT NULL DEFAULT 1,
    [LastRunTime] DATETIMEOFFSET NULL,
    [NextRunTime] DATETIMEOFFSET NULL,
    [TenantId] UNIQUEIDENTIFIER NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_sched_Schedule_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_sched_Schedule_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_Schedule_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Schedule_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Schedule_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_Schedule_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Schedule_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Schedule_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_Schedule] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Schedule_Name] UNIQUE ([Name])
);
GO

CREATE INDEX [IX_Schedule_ServiceOptionType] ON [sched].[Schedule]([ServiceOptionType]);
GO

CREATE INDEX [IX_Schedule_IsEnabled] ON [sched].[Schedule]([IsEnabled]) WHERE [IsEnabled] = 1;
GO

CREATE INDEX [IX_Schedule_NextRunTime] ON [sched].[Schedule]([NextRunTime]) WHERE [IsEnabled] = 1 AND [IsCurrent] = 1;
GO

CREATE INDEX [IX_Schedule_IsCurrent] ON [sched].[Schedule]([IsCurrent]) WHERE [IsCurrent] = 1;
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Job and pipeline schedules with cron or interval-based triggers.', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier for the schedule', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique display name for the schedule', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Schedule type discriminator (Cron, Interval)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'ServiceOptionType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the pipeline to execute', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'PipelineName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Cron expression for cron-based schedules', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'CronExpression';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Interval in seconds for interval-based schedules', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'IntervalSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'IANA time zone identifier for schedule calculations', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'TimeZoneId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether the schedule is currently enabled', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'IsEnabled';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the schedule last ran', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'LastRunTime';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calculated next run time', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'NextRunTime';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Tenant isolation identifier', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'TenantId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'Schedule', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
