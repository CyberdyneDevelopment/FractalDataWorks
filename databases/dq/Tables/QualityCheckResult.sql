CREATE TABLE [dq].[QualityCheckResult]
(
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_dq_QualityCheckResult_Id] DEFAULT NEWID(),
    [RuleId] UNIQUEIDENTIFIER NOT NULL,
    [Passed] BIT NOT NULL,
    [TotalRecords] INT NOT NULL,
    [PassedRecords] INT NOT NULL,
    [FailedRecords] INT NOT NULL,
    [PassRate] DECIMAL(5,4) NOT NULL,
    [ExecutedAt] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_dq_QualityCheckResult_ExecutedAt] DEFAULT (sysdatetimeoffset()),
    [SampleViolationsJson] NVARCHAR(MAX) NULL,
    [ErrorMessage] NVARCHAR(2000) NULL,
    CONSTRAINT [PK_QualityCheckResult] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE INDEX [IX_QualityCheckResult_RuleId] ON [dq].[QualityCheckResult]([RuleId]);
GO

CREATE INDEX [IX_QualityCheckResult_ExecutedAt] ON [dq].[QualityCheckResult]([ExecutedAt] DESC);
GO

CREATE INDEX [IX_QualityCheckResult_Passed] ON [dq].[QualityCheckResult]([Passed]);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Results from quality check executions. Stores pass/fail counts and sample violations.', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier for the check result', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Foreign key to the QualityRule.Id', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'RuleId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether all records passed the rule check', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'Passed';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Total number of records evaluated', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'TotalRecords';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of records that passed the check', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'PassedRecords';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of records that failed the check', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'FailedRecords';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Percentage of records that passed (0.0000 to 1.0000)', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'PassRate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the check was executed', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'ExecutedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of sample violations for debugging', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'SampleViolationsJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Error message if the check execution failed', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityCheckResult', @level2type=N'COLUMN', @level2name=N'ErrorMessage';
GO
