CREATE TABLE [cfg].[HttpConnection]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_HttpConnection_RowId] DEFAULT NEWID(),
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [BaseUrl] NVARCHAR(500) NULL,
    [TimeoutSeconds] INT NULL,
    [MaxRetries] INT NULL,
    [RetryDelayMs] INT NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [AcceptHeader] NVARCHAR(200) NULL,
    [ContentType] NVARCHAR(200) NULL,
    [AdditionalHeaders] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_HttpConnection_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_HttpConnection_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_HttpConnection_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_HttpConnection_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_HttpConnection_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_HttpConnection_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_HttpConnection_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_HttpConnection_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_HttpConnection] PRIMARY KEY ([RowId])
    -- No FK constraint: ConnectionId references Connection.Id which isn't unique (versioning)
);
GO

-- One current version per parent ConnectionId
CREATE UNIQUE INDEX [UX_HttpConnection_ConnectionId_Current] ON [cfg].[HttpConnection]([ConnectionId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_HttpConnection_ConnectionId] ON [cfg].[HttpConnection]([ConnectionId]);
GO

CREATE INDEX [IX_HttpConnection_IsCurrent] ON [cfg].[HttpConnection]([IsCurrent]);
GO

CREATE INDEX [IX_HttpConnection_IsDeleted] ON [cfg].[HttpConnection]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'HTTP/REST connection configuration with row-level versioning. Child of cfg.Connection.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'HttpConnection';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Connection.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'ConnectionId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Base URL for HTTP requests.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'BaseUrl';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Request timeout in seconds.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'TimeoutSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Maximum retry attempts for failed requests.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'MaxRetries';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Delay in milliseconds between retry attempts.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'RetryDelayMs';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User-Agent header value.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'UserAgent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Accept header value.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'AcceptHeader';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Content-Type header value.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'ContentType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON-serialized additional HTTP headers.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'AdditionalHeaders';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'HttpConnection', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
