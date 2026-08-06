CREATE TABLE [dq].[QualityRule]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_dq_QualityRule_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [DataSetName] NVARCHAR(200) NOT NULL,
    [FieldName] NVARCHAR(200) NULL,
    [RuleType] NVARCHAR(64) NOT NULL,
    [Severity] NVARCHAR(32) NOT NULL DEFAULT 'Error',
    [IsEnabled] BIT NOT NULL DEFAULT 1,
    [Description] NVARCHAR(500) NULL,
    [MinValue] NVARCHAR(256) NULL,
    [MaxValue] NVARCHAR(256) NULL,
    [Pattern] NVARCHAR(1000) NULL,
    [Expression] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_dq_QualityRule_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_dq_QualityRule_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_dq_QualityRule_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_dq_QualityRule_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_dq_QualityRule_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_dq_QualityRule_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_dq_QualityRule_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_dq_QualityRule_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_QualityRule] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_QualityRule_Id_Current] ON [dq].[QualityRule]([Id]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_QualityRule_DataSetName] ON [dq].[QualityRule]([DataSetName]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;
GO

CREATE INDEX [IX_QualityRule_RuleType] ON [dq].[QualityRule]([RuleType]);
GO

CREATE INDEX [IX_QualityRule_Severity] ON [dq].[QualityRule]([Severity]);
GO

CREATE INDEX [IX_QualityRule_IsCurrent] ON [dq].[QualityRule]([IsCurrent]);
GO

CREATE INDEX [IX_QualityRule_IsDeleted] ON [dq].[QualityRule]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Data quality rules for validating DataSet records. Supports field-level and aggregate rules.', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key)', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the rule (persists across versions)', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the DataSet this rule applies to', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'DataSetName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional field name for field-level rules (NULL for aggregate rules)', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'FieldName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Rule type from QualityRuleTypes TypeCollection (NotNull, Unique, InRange, etc.)', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'RuleType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Severity level from QualitySeverityTypes (Error, Warning, Info)', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'Severity';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether the rule is enabled for execution', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'IsEnabled';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Human-readable description of the rule', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Minimum value for InRange rules', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'MinValue';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Maximum value for InRange rules', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'MaxValue';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Regex pattern for MatchesPattern rules', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'Pattern';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom expression for CustomExpression rules', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'Expression';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag', @level0type=N'SCHEMA', @level0name=N'dq', @level1type=N'TABLE', @level1name=N'QualityRule', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO
