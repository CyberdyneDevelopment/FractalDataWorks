CREATE SCHEMA [meta]
    AUTHORIZATION [dbo];
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Metadata and catalog schema for business glossary, annotations, and data lineage', @level0type=N'SCHEMA', @level0name=N'meta';
GO
