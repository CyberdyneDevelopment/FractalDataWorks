-- types schema for TypeCollection metadata persistence
-- FractalDataWorks TypeCollection persistence layer

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'types')
    EXEC('CREATE SCHEMA types')
GO

-- TypeCollection table: stores metadata about each TypeCollection
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'types.TypeCollection') AND type = N'U')
CREATE TABLE types.TypeCollection
(
     Id                 INT                 NOT NULL
    ,Name               VARCHAR(100)        NOT NULL
    ,FullName           VARCHAR(500)        NOT NULL
    ,CollectionKind     VARCHAR(50)         NOT NULL
    ,ServiceCategory    VARCHAR(100)        NULL
    ,AssemblyName       VARCHAR(500)        NULL
    ,IsCurrent          BIT                 NOT NULL    DEFAULT 1
    ,CreateDate         DATETIMEOFFSET      NOT NULL    DEFAULT SYSDATETIMEOFFSET()
    ,ModifyDate         DATETIMEOFFSET      NULL

    ,CONSTRAINT PK_TypeCollection PRIMARY KEY (Id)
    ,CONSTRAINT UQ_TypeCollection_FullName UNIQUE (FullName)
)
GO

-- TypeOption table: stores metadata about each TypeOption in a collection
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'types.TypeOption') AND type = N'U')
CREATE TABLE types.TypeOption
(
     Id                 INT                 NOT NULL
    ,TypeCollectionId   INT                 NOT NULL
    ,Name               VARCHAR(100)        NOT NULL
    ,FullTypeName       VARCHAR(500)        NOT NULL
    ,Category           VARCHAR(100)        NULL
    ,Description        VARCHAR(1000)       NULL
    ,IsCurrent          BIT                 NOT NULL    DEFAULT 1
    ,CreateDate         DATETIMEOFFSET      NOT NULL    DEFAULT SYSDATETIMEOFFSET()

    ,CONSTRAINT PK_TypeOption PRIMARY KEY (Id)
    ,CONSTRAINT FK_TypeOption_Collection FOREIGN KEY (TypeCollectionId)
        REFERENCES types.TypeCollection (Id)
    ,CONSTRAINT UQ_TypeOption_Name UNIQUE (TypeCollectionId, Name)
)
GO

CREATE INDEX IX_TypeOption_CollectionId ON types.TypeOption (TypeCollectionId)
GO

-- TypeProperty table: stores property metadata for each TypeOption
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'types.TypeProperty') AND type = N'U')
CREATE TABLE types.TypeProperty
(
     Id                 INT IDENTITY(1,1)   NOT NULL
    ,TypeOptionId       INT                 NOT NULL
    ,PropertyName       VARCHAR(100)        NOT NULL
    ,PropertyType       VARCHAR(500)        NOT NULL
    ,PropertyRole       VARCHAR(50)         NULL
    ,SqlType            VARCHAR(100)        NULL
    ,MaxLength          INT                 NULL
    ,IsNullable         BIT                 NOT NULL    DEFAULT 1
    ,IsCollection       BIT                 NOT NULL    DEFAULT 0
    ,IsCurrent          BIT                 NOT NULL    DEFAULT 1

    ,CONSTRAINT PK_TypeProperty PRIMARY KEY (Id)
    ,CONSTRAINT FK_TypeProperty_Option FOREIGN KEY (TypeOptionId)
        REFERENCES types.TypeOption (Id)
    ,CONSTRAINT UQ_TypeProperty_Name UNIQUE (TypeOptionId, PropertyName)
)
GO

CREATE INDEX IX_TypeProperty_OptionId ON types.TypeProperty (TypeOptionId)
GO
