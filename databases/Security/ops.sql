CREATE SCHEMA [ops]
    AUTHORIZATION [dbo];
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Operations schema for workflow execution, monitoring, and alerting', @level0type=N'SCHEMA', @level0name=N'ops';
GO
