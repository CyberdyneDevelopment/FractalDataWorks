CREATE SCHEMA [dq]
    AUTHORIZATION [dbo];
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Data Quality schema for quality rules, checks, and validation results', @level0type=N'SCHEMA', @level0name=N'dq';
GO
