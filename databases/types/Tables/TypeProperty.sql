CREATE TABLE [types].[TypeProperty]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TypeOptionId] INT NOT NULL,
    [PropertyName] VARCHAR(100) NOT NULL,
    [PropertyType] VARCHAR(500) NOT NULL,
    [PropertyRole] VARCHAR(50) NULL,
    [SqlType] VARCHAR(100) NULL,
    [MaxLength] INT NULL,
    [IsNullable] BIT NOT NULL CONSTRAINT [DF_types_TypeProperty_IsNullable] DEFAULT (1),
    [IsCollection] BIT NOT NULL CONSTRAINT [DF_types_TypeProperty_IsCollection] DEFAULT (0),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_types_TypeProperty_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_types_TypeProperty_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_types_TypeProperty_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeProperty_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeProperty_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_types_TypeProperty_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeProperty_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeProperty_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_TypeProperty] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TypeProperty_Option] FOREIGN KEY ([TypeOptionId])
        REFERENCES [types].[TypeOption]([Id])
);
GO

CREATE INDEX [IX_TypeProperty_TypeOptionId] ON [types].[TypeProperty]([TypeOptionId]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'TypeProperty metadata for properties of TypeOptions. Enables runtime introspection and schema generation.',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Unique identifier for the TypeProperty',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Foreign key to the parent TypeOption',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'TypeOptionId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Name of the property on the TypeOption class',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'PropertyName';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Fully qualified .NET type of the property',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'PropertyType';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Semantic role of the property (Key, Name, Value, etc.)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'PropertyRole';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'SQL Server data type for database schema generation',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'SqlType';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Maximum length for string or collection types',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'MaxLength';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Whether the property accepts null values',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'IsNullable';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Whether the property is a collection type (IList, IEnumerable, etc.)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'IsCollection';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this is the current version of the record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this record has been soft deleted',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Original creation date from source system (if migrated)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was created',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that created this record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was created',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that last modified this record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeProperty',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
