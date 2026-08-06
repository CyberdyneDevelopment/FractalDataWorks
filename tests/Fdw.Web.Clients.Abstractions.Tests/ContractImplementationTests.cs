using Fdw.Web.Clients.Abstractions.Contracts;

using SchemaPreviewRequest = Fdw.Schema.Clients.Models.SchemaPreviewRequest;

using DataColumnDto = Fdw.Services.Data.Clients.Models.ColumnSchemaPayload;
using DataFieldResult = Fdw.Services.Data.Clients.Models.FieldDiscoveryResult;
using DataContainerResult = Fdw.Services.Data.Clients.Models.ContainerDiscoveryResult;
using DataPreviewRequestPayload = Fdw.Services.Data.Clients.Models.DataPreviewRequestPayload;
using DataPreviewResponsePayload = Fdw.Services.Data.Clients.Models.DataPreviewResponsePayload;
using DataDataSetFieldDto = Fdw.Services.Data.Clients.Models.DataSetFieldPayload;

using CalcDataSetFieldDto = Fdw.Web.Calculations.Clients.Models.DataSetFieldPayload;
using CalcTypeStats = Fdw.Web.Calculations.Clients.Models.CalculationTypeStats;

using AnalyticsEnvironmentDto = Fdw.Web.Analytics.Clients.Models.EnvironmentPayload;
using AnalyticsCalcTypeStats = Fdw.Web.Analytics.Clients.Models.CalculationTypeStats;

using OpsEnvironmentDto = Fdw.Operations.Clients.Models.EnvironmentPayload;

namespace Fdw.Web.Clients.Abstractions.Tests;

public sealed class ContractImplementationTests
{
    // -----------------------------------------------------------------------
    // Schema.Clients: SchemaPreviewRequest -> IDataPreviewRequest
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SchemaDataPreviewRequestImplementsIDataPreviewRequest()
    {
        var dto = new SchemaPreviewRequest();
        IDataPreviewRequest contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SchemaDataPreviewRequestPropertiesAccessibleThroughInterface()
    {
        var dto = new SchemaPreviewRequest
        {
            DataSetName = "Sales",
            DataStoreName = "ProdDb",
            PathName = "dbo",
            ContainerName = "Orders",
            MaxRows = 50
        };

        IDataPreviewRequest contract = dto;

        contract.DataSetName.ShouldBe("Sales");
        contract.DataStoreName.ShouldBe("ProdDb");
        contract.PathName.ShouldBe("dbo");
        contract.ContainerName.ShouldBe("Orders");
        contract.MaxRows.ShouldBe(50);
    }

    // -----------------------------------------------------------------------
    // Data.Clients: ColumnSchemaPayload -> IColumnSchema
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataColumnSchemaDtoImplementsIColumnSchema()
    {
        var dto = new DataColumnDto();
        IColumnSchema contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataColumnSchemaDtoPropertiesAccessibleThroughInterface()
    {
        var dto = new DataColumnDto
        {
            Name = "ProductName",
            DataType = "varchar",
            IsNullable = true,
            MaxLength = 200,
            Precision = null,
            Scale = null,
            Role = "Display"
        };

        IColumnSchema contract = dto;

        contract.Name.ShouldBe("ProductName");
        contract.DataType.ShouldBe("varchar");
        contract.IsNullable.ShouldBeTrue();
        contract.MaxLength.ShouldBe(200);
        contract.Precision.ShouldBeNull();
        contract.Scale.ShouldBeNull();
        contract.Role.ShouldBe("Display");
    }

    // -----------------------------------------------------------------------
    // Data.Clients: FieldDiscoveryResult -> IFieldDiscovery
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataFieldDiscoveryResultImplementsIFieldDiscovery()
    {
        var dto = new DataFieldResult();
        IFieldDiscovery contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataFieldDiscoveryResultPropertiesAccessibleThroughInterface()
    {
        var dto = new DataFieldResult
        {
            Name = "Timestamp",
            DataType = "datetime2",
            IsNullable = false,
            IsKey = false
        };

        IFieldDiscovery contract = dto;

        contract.Name.ShouldBe("Timestamp");
        contract.DataType.ShouldBe("datetime2");
        contract.IsNullable.ShouldBeFalse();
        contract.IsKey.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // Data.Clients: ContainerDiscoveryResult -> IContainerDiscovery
    // (explicit interface implementation for Fields)
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataContainerDiscoveryResultImplementsIContainerDiscovery()
    {
        var dto = new DataContainerResult();
        IContainerDiscovery contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataContainerDiscoveryResultFieldsAccessibleThroughInterface()
    {
        var field = new DataFieldResult
        {
            Name = "Id",
            DataType = "int",
            IsNullable = false,
            IsKey = true
        };

        var dto = new DataContainerResult
        {
            Name = "Inventory",
            ContainerType = "View",
            Fields = [field]
        };

        IContainerDiscovery contract = dto;

        contract.Name.ShouldBe("Inventory");
        contract.ContainerType.ShouldBe("View");
        contract.Fields.Count.ShouldBe(1);
        contract.Fields[0].Name.ShouldBe("Id");
        contract.Fields[0].IsKey.ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // Data.Clients: DataPreviewRequestPayload -> IDataPreviewRequest
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataPreviewRequestImplementsIDataPreviewRequest()
    {
        var dto = new DataPreviewRequestPayload();
        IDataPreviewRequest contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataPreviewRequestPropertiesAccessibleThroughInterface()
    {
        var dto = new DataPreviewRequestPayload
        {
            DataSetName = "Inventory",
            DataStoreName = "WarehouseDb",
            PathName = "warehouse",
            ContainerName = "Products",
            MaxRows = 25
        };

        IDataPreviewRequest contract = dto;

        contract.DataSetName.ShouldBe("Inventory");
        contract.DataStoreName.ShouldBe("WarehouseDb");
        contract.PathName.ShouldBe("warehouse");
        contract.ContainerName.ShouldBe("Products");
        contract.MaxRows.ShouldBe(25);
    }

    // -----------------------------------------------------------------------
    // Data.Clients: DataPreviewResponsePayload -> IDataPreviewResponse
    // (explicit interface implementation for Columns)
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataPreviewResponseImplementsIDataPreviewResponse()
    {
        var dto = new DataPreviewResponsePayload();
        IDataPreviewResponse contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataPreviewResponseColumnsAccessibleThroughInterface()
    {
        var column = new DataColumnDto
        {
            Name = "Quantity",
            DataType = "int",
            IsNullable = false,
        };

        var dto = new DataPreviewResponsePayload
        {
            Columns = [column],
            Rows = [],
            TotalRowCount = null,
            HasMoreRows = false
        };

        IDataPreviewResponse contract = dto;

        contract.Columns.Count.ShouldBe(1);
        contract.Columns[0].Name.ShouldBe("Quantity");
        contract.Rows.ShouldBeEmpty();
        contract.TotalRowCount.ShouldBeNull();
        contract.HasMoreRows.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // Data.Clients: DataSetFieldPayload -> IDataSetField
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataDataSetFieldDtoImplementsIDataSetField()
    {
        var dto = new DataDataSetFieldDto();
        IDataSetField contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DataDataSetFieldDtoPropertiesAccessibleThroughInterface()
    {
        var id = Guid.NewGuid();
        var dto = new DataDataSetFieldDto
        {
            Id = id,
            Name = "Revenue",
            Description = "Total revenue",
            DataType = "decimal",
            IsKey = false,
            IsRequired = true,
            IsIndexed = true,
            MaxLength = null,
            DefaultValue = "0",
            IsCalculated = true,
            Role = "Measure",
            Ordinal = 5
        };

        IDataSetField contract = dto;

        contract.Id.ShouldBe(id);
        contract.Name.ShouldBe("Revenue");
        contract.Description.ShouldBe("Total revenue");
        contract.DataType.ShouldBe("decimal");
        contract.IsKey.ShouldBeFalse();
        contract.IsRequired.ShouldBeTrue();
        contract.IsIndexed.ShouldBeTrue();
        contract.MaxLength.ShouldBeNull();
        contract.DefaultValue.ShouldBe("0");
        contract.IsCalculated.ShouldBeTrue();
        contract.Role.ShouldBe("Measure");
        contract.Ordinal.ShouldBe(5);
    }

    // -----------------------------------------------------------------------
    // Calculations.Clients: DataSetFieldPayload -> IDataSetField
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CalcDataSetFieldDtoImplementsIDataSetField()
    {
        var dto = new CalcDataSetFieldDto();
        IDataSetField contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CalcDataSetFieldDtoPropertiesAccessibleThroughInterface()
    {
        var id = Guid.NewGuid();
        var dto = new CalcDataSetFieldDto
        {
            Id = id,
            Name = "Score",
            Description = "Computed score",
            DataType = "float",
            IsKey = false,
            IsRequired = false,
            IsIndexed = false,
            MaxLength = null,
            DefaultValue = null,
            IsCalculated = true,
            Role = null,
            Ordinal = 10
        };

        IDataSetField contract = dto;

        contract.Id.ShouldBe(id);
        contract.Name.ShouldBe("Score");
        contract.Description.ShouldBe("Computed score");
        contract.DataType.ShouldBe("float");
        contract.IsKey.ShouldBeFalse();
        contract.IsRequired.ShouldBeFalse();
        contract.IsIndexed.ShouldBeFalse();
        contract.MaxLength.ShouldBeNull();
        contract.DefaultValue.ShouldBeNull();
        contract.IsCalculated.ShouldBeTrue();
        contract.Role.ShouldBeNull();
        contract.Ordinal.ShouldBe(10);
    }

    // -----------------------------------------------------------------------
    // Calculations.Clients: CalculationTypeStats -> ICalculationTypeStats
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CalcCalculationTypeStatsImplementsICalculationTypeStats()
    {
        var dto = new CalcTypeStats();
        ICalculationTypeStats contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CalcCalculationTypeStatsPropertiesAccessibleThroughInterface()
    {
        var lastExec = DateTimeOffset.UtcNow;
        var dto = new CalcTypeStats
        {
            CalculationType = "Aggregate",
            ExecutionCount = 500L,
            AverageDurationMs = 42.5,
            MinDurationMs = 1.2,
            MaxDurationMs = 350.0,
            SuccessRate = 98.5,
            CacheHitRate = 72.3,
            LastExecuted = lastExec
        };

        ICalculationTypeStats contract = dto;

        contract.CalculationType.ShouldBe("Aggregate");
        contract.ExecutionCount.ShouldBe(500L);
        contract.AverageDurationMs.ShouldBe(42.5);
        contract.MinDurationMs.ShouldBe(1.2);
        contract.MaxDurationMs.ShouldBe(350.0);
        contract.SuccessRate.ShouldBe(98.5);
        contract.CacheHitRate.ShouldBe(72.3);
        contract.LastExecuted.ShouldBe(lastExec);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CalcCalculationTypeStatsLastExecutedNullWhenNeverRun()
    {
        var dto = new CalcTypeStats
        {
            CalculationType = "Transform",
            LastExecuted = null
        };

        ICalculationTypeStats contract = dto;

        contract.LastExecuted.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // Analytics.Clients: EnvironmentPayload -> IEnvironmentInfo
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnalyticsEnvironmentDtoImplementsIEnvironmentInfo()
    {
        var dto = new AnalyticsEnvironmentDto();
        IEnvironmentInfo contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnalyticsEnvironmentDtoPropertiesAccessibleThroughInterface()
    {
        var id = Guid.NewGuid();
        var dto = new AnalyticsEnvironmentDto
        {
            Id = id,
            Name = "Production",
            Description = "Live production environment"
        };

        IEnvironmentInfo contract = dto;

        contract.Id.ShouldBe(id);
        contract.Name.ShouldBe("Production");
        contract.Description.ShouldBe("Live production environment");
    }

    // -----------------------------------------------------------------------
    // Analytics.Clients: CalculationTypeStats -> ICalculationTypeStats
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnalyticsCalculationTypeStatsImplementsICalculationTypeStats()
    {
        var dto = new AnalyticsCalcTypeStats();
        ICalculationTypeStats contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnalyticsCalculationTypeStatsPropertiesAccessibleThroughInterface()
    {
        var lastExec = new DateTimeOffset(2026, 2, 15, 10, 30, 0, TimeSpan.Zero);
        var dto = new AnalyticsCalcTypeStats
        {
            CalculationType = "Prediction",
            ExecutionCount = 1200L,
            AverageDurationMs = 150.7,
            MinDurationMs = 10.0,
            MaxDurationMs = 2500.0,
            SuccessRate = 95.0,
            CacheHitRate = 60.0,
            LastExecuted = lastExec
        };

        ICalculationTypeStats contract = dto;

        contract.CalculationType.ShouldBe("Prediction");
        contract.ExecutionCount.ShouldBe(1200L);
        contract.AverageDurationMs.ShouldBe(150.7);
        contract.MinDurationMs.ShouldBe(10.0);
        contract.MaxDurationMs.ShouldBe(2500.0);
        contract.SuccessRate.ShouldBe(95.0);
        contract.CacheHitRate.ShouldBe(60.0);
        contract.LastExecuted.ShouldBe(lastExec);
    }

    // -----------------------------------------------------------------------
    // Operations.Clients: EnvironmentPayload -> IEnvironmentInfo
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OpsEnvironmentDtoImplementsIEnvironmentInfo()
    {
        var dto = new OpsEnvironmentDto();
        IEnvironmentInfo contract = dto;
        contract.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OpsEnvironmentDtoPropertiesAccessibleThroughInterface()
    {
        var id = Guid.NewGuid();
        var dto = new OpsEnvironmentDto
        {
            Id = id,
            Name = "Staging",
            Description = "Pre-production staging environment"
        };

        IEnvironmentInfo contract = dto;

        contract.Id.ShouldBe(id);
        contract.Name.ShouldBe("Staging");
        contract.Description.ShouldBe("Pre-production staging environment");
    }
}
