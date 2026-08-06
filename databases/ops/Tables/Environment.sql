CREATE TABLE [ops].[Environment]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ops_Environment_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(64) NOT NULL,
    [PromotionOrder] INT NOT NULL,
    [ConnectionString] NVARCHAR(1000) NULL,
    [RequiresApproval] BIT NOT NULL DEFAULT 0,
    [ApproversJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_ops_Environment_ApproversJson] DEFAULT '[]',
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_ops_Environment_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ops_Environment_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_Environment_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_Environment_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_Environment_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_ops_Environment_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_Environment_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_ops_Environment_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_Environment] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_Environment_Id_Current] ON [ops].[Environment]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per Name
CREATE UNIQUE INDEX [UX_Environment_Name_Current] ON [ops].[Environment]([Name]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;
GO

CREATE INDEX [IX_Environment_PromotionOrder] ON [ops].[Environment]([PromotionOrder]);
GO

CREATE INDEX [IX_Environment_IsCurrent] ON [ops].[Environment]([IsCurrent]);
GO

CREATE INDEX [IX_Environment_IsDeleted] ON [ops].[Environment]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Deployment environments for configuration promotion (Dev, Staging, Prod).', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key)', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the environment (persists across versions)', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique environment name (Dev, Staging, Production)', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Order in the promotion chain (0=Dev, 1=Staging, 2=Prod)', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'PromotionOrder';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Connection string to the environment database', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'ConnectionString';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether promotions to this environment require approval', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'RequiresApproval';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of users/groups who can approve promotions', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'ApproversJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag', @level0type=N'SCHEMA', @level0name=N'ops', @level1type=N'TABLE', @level1name=N'Environment', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO
