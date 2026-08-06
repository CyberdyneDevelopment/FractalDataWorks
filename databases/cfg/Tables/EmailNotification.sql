CREATE TABLE [cfg].[EmailNotification]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_EmailNotification_RowId] DEFAULT NEWID(),
    [NotificationId] UNIQUEIDENTIFIER NOT NULL,
    [SmtpServer] NVARCHAR(200) NOT NULL,
    [SmtpPort] INT NOT NULL CONSTRAINT [DF_cfg_EmailNotification_SmtpPort] DEFAULT (587),
    [FromAddress] NVARCHAR(256) NOT NULL,
    [ToAddresses] NVARCHAR(MAX) NOT NULL,
    [UseSsl] BIT NOT NULL CONSTRAINT [DF_cfg_EmailNotification_UseSsl] DEFAULT (1),
    [RequireAuthentication] BIT NOT NULL CONSTRAINT [DF_cfg_EmailNotification_RequireAuthentication] DEFAULT (1),
    [SecretManagerName] NVARCHAR(200) NULL,
    [SecretKeyName] NVARCHAR(200) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_EmailNotification_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_EmailNotification_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_EmailNotification_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EmailNotification_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EmailNotification_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_EmailNotification_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EmailNotification_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_EmailNotification_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_EmailNotification] PRIMARY KEY ([RowId])
    -- No FK constraint: NotificationId references Notification.Id which isn't unique (versioning)
);
GO

-- One current version per parent NotificationId
CREATE UNIQUE INDEX [UX_EmailNotification_NotificationId_Current] ON [cfg].[EmailNotification]([NotificationId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_EmailNotification_NotificationId] ON [cfg].[EmailNotification]([NotificationId]);
GO

CREATE INDEX [IX_EmailNotification_SmtpServer] ON [cfg].[EmailNotification]([SmtpServer]);
GO

CREATE INDEX [IX_EmailNotification_IsCurrent] ON [cfg].[EmailNotification]([IsCurrent]);
GO

CREATE INDEX [IX_EmailNotification_IsDeleted] ON [cfg].[EmailNotification]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Email notification configuration with row-level versioning. Child of cfg.Notification.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'EmailNotification';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Notification.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'NotificationId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SMTP server hostname or IP address.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'SmtpServer';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SMTP server port number.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'SmtpPort';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Email address used as sender.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'FromAddress';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Comma-separated recipient email addresses.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'ToAddresses';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Use SSL/TLS encryption.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'UseSsl';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Require SMTP authentication.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'RequireAuthentication';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the SecretManager for SMTP credentials.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'SecretManagerName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Secret key name for SMTP credentials.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'SecretKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'EmailNotification', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
