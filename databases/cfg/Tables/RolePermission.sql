CREATE TABLE [cfg].[RolePermission]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_RolePermission_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] UNIQUEIDENTIFIER NOT NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_RolePermission_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_RolePermission_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_RolePermission_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_RolePermission_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_RolePermission_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_RolePermission_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_RolePermission_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_RolePermission_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_RolePermission] PRIMARY KEY ([RowId])
    -- No FK constraints: RoleId/PermissionId reference logical Ids which aren't unique (versioning)
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_RolePermission_Id_Current] ON [cfg].[RolePermission]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per Role+Permission combination
CREATE UNIQUE INDEX [UX_RolePermission_RoleId_PermissionId_Current] ON [cfg].[RolePermission]([RoleId], [PermissionId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_RolePermission_Id] ON [cfg].[RolePermission]([Id]);
GO

CREATE INDEX [IX_RolePermission_RoleId] ON [cfg].[RolePermission]([RoleId]);
GO

CREATE INDEX [IX_RolePermission_PermissionId] ON [cfg].[RolePermission]([PermissionId]);
GO

CREATE INDEX [IX_RolePermission_IsCurrent] ON [cfg].[RolePermission]([IsCurrent]);
GO

CREATE INDEX [IX_RolePermission_IsDeleted] ON [cfg].[RolePermission]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Role-permission assignments with row-level versioning. Many-to-many relationship between roles and permissions.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'RolePermission';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the role-permission assignment (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Role.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'RoleId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Permission.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'PermissionId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'RolePermission', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
