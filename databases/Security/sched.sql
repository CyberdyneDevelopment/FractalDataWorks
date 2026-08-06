CREATE SCHEMA [sched]
    AUTHORIZATION [dbo];
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Scheduling schema for job and pipeline execution management', @level0type=N'SCHEMA', @level0name=N'sched';
GO
