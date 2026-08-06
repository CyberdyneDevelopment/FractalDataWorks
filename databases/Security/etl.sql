CREATE SCHEMA [etl]
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'ETL pipeline configurations and execution tracking',
    @level0type=N'SCHEMA', @level0name=N'etl'
GO
