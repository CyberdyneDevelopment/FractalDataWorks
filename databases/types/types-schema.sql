-- Create types schema for TypeCollection metadata persistence
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'types')
BEGIN
    EXEC('CREATE SCHEMA types')
END
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'TypeCollection metadata for runtime type discovery. Stores metadata about TypeCollections, TypeOptions, and their properties for dynamic introspection and schema generation.',
    @level0type=N'SCHEMA', @level0name=N'types';
GO
