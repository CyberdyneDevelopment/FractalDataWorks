CREATE TABLE [ops].[PromotionRequest]
(
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ops_PromotionRequest_Id] DEFAULT NEWID(),
    [SourceEnvironment] NVARCHAR(64) NOT NULL,
    [TargetEnvironment] NVARCHAR(64) NOT NULL,
    [ItemsJson] NVARCHAR(MAX) NOT NULL,
    [RequestedBy] NVARCHAR(256) NOT NULL,
    [Notes] NVARCHAR(1000) NULL,
    [Status] NVARCHAR(32) NOT NULL DEFAULT 'Pending',
    [ApprovedBy] NVARCHAR(256) NULL,
    [ApprovedAt] DATETIMEOFFSET(7) NULL,
    [CompletedAt] DATETIMEOFFSET(7) NULL,
    [ErrorMessage] NVARCHAR(2000) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_PromotionRequest_CreateDate] DEFAULT (sysdatetimeoffset()),
    CONSTRAINT [PK_PromotionRequest] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE INDEX [IX_PromotionRequest_SourceEnvironment] ON [ops].[PromotionRequest]([SourceEnvironment]);
GO

CREATE INDEX [IX_PromotionRequest_TargetEnvironment] ON [ops].[PromotionRequest]([TargetEnvironment]);
GO

CREATE INDEX [IX_PromotionRequest_Status] ON [ops].[PromotionRequest]([Status]);
GO

CREATE INDEX [IX_PromotionRequest_RequestedBy] ON [ops].[PromotionRequest]([RequestedBy]);
GO

CREATE INDEX [IX_PromotionRequest_CreateDate] ON [ops].[PromotionRequest]([CreateDate] DESC);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Configuration promotion requests with approval workflow. Tracks status from Pending through Completed/Failed.', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier for the promotion request', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Environment to promote from', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'SourceEnvironment';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Environment to promote to', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'TargetEnvironment';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of items to promote (type and name)', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'ItemsJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User who requested the promotion', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'RequestedBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional notes about the promotion', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'Notes';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Status from PromotionStatusTypes (Pending, Approved, Rejected, InProgress, Completed, Failed)', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'Status';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User who approved/rejected the promotion', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'ApprovedBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the promotion was approved/rejected', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'ApprovedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the promotion completed or failed', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'CompletedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Error message if the promotion failed', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'ErrorMessage';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the promotion was requested', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'PromotionRequest', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO
