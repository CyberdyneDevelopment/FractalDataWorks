CREATE SCHEMA [types]
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'TypeCollection metadata for runtime type discovery and introspection',
    @level0type=N'SCHEMA', @level0name=N'types'
GO
