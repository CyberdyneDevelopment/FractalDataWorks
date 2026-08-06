using Fdw.Data.SchemaImporters.Abstractions;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;

namespace Fdw.Data.Importers.Abstractions.Tests;

public class SchemaImporterBaseTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        var sut = new TestSchemaImporter();

        sut.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        var sut = new TestSchemaImporter();

        sut.Name.ShouldBe("TestImporter");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDescription()
    {
        var sut = new TestSchemaImporter();

        sut.Description.ShouldBe("Test schema importer");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDataStoreType()
    {
        var sut = new TestSchemaImporter();

        sut.DataStoreType.ShouldBe("SqlServer");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsConfigurationKey()
    {
        var sut = new TestSchemaImporter();

        sut.ConfigurationKey.ShouldBe("SchemaImporters:TestImporter");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateReturnsFailureForNullSource()
    {
        var sut = new TestSchemaImporter();

        var result = await sut.Validate(null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateReturnsFailureForEmptySource()
    {
        var sut = new TestSchemaImporter();

        var result = await sut.Validate(string.Empty, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateReturnsFailureForWhitespaceSource()
    {
        var sut = new TestSchemaImporter();

        var result = await sut.Validate("   ", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateReturnsSuccessForValidSource()
    {
        var sut = new TestSchemaImporter();

        var result = await sut.Validate("Server=localhost", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateFailureUsesSourceRequiredResultCode()
    {
        var sut = new TestSchemaImporter();

        var result = await sut.Validate("", TestContext.Current.CancellationToken);

        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("SourceRequired");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ImportReturnsDiscoveredDataStoreConfiguration()
    {
        var discovered = new DataStoreConfiguration { Name = "Discovered" };
        var sut = new TestSchemaImporter { ImportResult = GenericResult<DataStoreConfiguration>.Success(discovered) };

        var result = await sut.Import("test-source", cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(discovered);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ImportPropagatesFailure()
    {
        var sut = new TestSchemaImporter
        {
            ImportResult = GenericResult<DataStoreConfiguration>.Failure(
                SchemaImporters.Abstractions.Results.SchemaImporterResultCodes.ByName("ImportFailed"))
        };

        var result = await sut.Import("test-source", cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ImportFailed");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ImportPassesSource()
    {
        var sut = new TestSchemaImporter { ImportResult = GenericResult<DataStoreConfiguration>.Success(new DataStoreConfiguration()) };

        await sut.Import("my-source", cancellationToken: TestContext.Current.CancellationToken);

        sut.LastImportSource.ShouldBe("my-source");
    }

    public sealed class TestConfig
    {
        public string Server { get; set; } = string.Empty;
    }

    private sealed class TestSchemaImporter : SchemaImporterBase<TestConfig>
    {
        public IGenericResult<DataStoreConfiguration>? ImportResult { get; set; }
        public string? LastImportSource { get; private set; }

        public TestSchemaImporter()
            : base(1, "TestImporter", "Test schema importer", "SqlServer")
        {
        }

        public override Task<IGenericResult<DataStoreConfiguration>> Import(
            string source,
            SchemaImporterOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastImportSource = source;
            return Task.FromResult(ImportResult ?? GenericResult<DataStoreConfiguration>.Failure());
        }
    }
}
