CREATE SCHEMA [cfg]
    AUTHORIZATION [dbo];
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Configuration schema for FractalDataWorks service and application settings', @level0type=N'SCHEMA', @level0name=N'cfg';
GO
