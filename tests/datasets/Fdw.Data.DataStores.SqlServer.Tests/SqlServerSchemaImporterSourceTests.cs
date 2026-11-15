using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.SqlServer.Models;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataStores.SqlServer.Tests;

/// <summary>
/// FDW-5951: the SQL Server schema importer's <c>source</c> is the name of a configured connection, resolved with the
/// <see cref="IDataConnectionProvider"/> the options carry, and the importer reads the catalog through that connection. It never opens SQL Server
/// itself and never takes a connection string. Each refusal is its own message: an empty source, a value that is a connection string, options
/// with no provider, a name the provider does not resolve. A failure of the provider passes up with its own cause.
/// </summary>
public sealed class SqlServerSchemaImporterSourceTests
{
    private readonly Mock<IDataConnectionProvider> _provider = new();
    private readonly Mock<IDataConnection> _connection = new();

    public SqlServerSchemaImporterSourceTests()
    {
        // A database "Shop" with nothing in it: the catalog names the database, and no table, view or procedure.
        Serve(new List<InformationSchemaSchemata> { new() { CatalogName = "Shop", SchemaName = "dbo" } });
        Serve(new List<InformationSchemaTable>());
        Serve(new List<InformationSchemaRoutine>());
    }

    private void Serve<TRow>(List<TRow> rows)
        => _connection
            .Setup(c => c.Execute<IEnumerable<TRow>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TRow>>.Success(rows));

    private static SqlServerSchemaImporter CreateSut() => new(NullLogger<SqlServerSchemaImporter>.Instance);

    private SchemaImporterOptions OptionsWithProvider()
        => new() { IncludeExtendedProperties = false, ConnectionProvider = _provider.Object };

    private static SchemaImporterOptions OptionsWithoutProvider() => new() { IncludeExtendedProperties = false };

    private void NeverAsksTheProvider()
        => _provider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task ImportRefusesASourceThatNamesNoConnectionAndNeverAsksTheProvider(string source)
    {
        var result = await CreateSut().Import(source, OptionsWithProvider(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("names no connection");
        NeverAsksTheProvider();
    }

    [Theory]
    [InlineData("Server=db;Database=Shop;User Id=sa;Password=x")]
    [InlineData("Data Source=.;Integrated Security=true")]
    [InlineData("Shop;Trusted_Connection=true")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ImportRefusesAConnectionStringAndNeverAsksTheProviderOrOpensIt(string source)
    {
        // Why: a connection string reaching the importer is the old contract; it would carry a password to a log or an error message, and
        // nothing may open SQL Server outside a configured connection.
        var result = await CreateSut().Import(source, OptionsWithProvider(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("is a connection string");
        result.CurrentMessage.ShouldNotContain("Password", Case.Insensitive);
        NeverAsksTheProvider();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task ImportRefusesAConnectionNameWhenTheOptionsCarryNoProviderAndNamesTheMissingProvider()
    {
        var result = await CreateSut().Import("Shop", OptionsWithoutProvider(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("no connection provider");
        result.CurrentMessage.ShouldContain("IDataConnectionProvider");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task ImportKeepsTheCauseWhenTheProviderFails()
    {
        _provider
            .Setup(p => p.Get("Shop", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Failure(new GenericMessage(MessageSeverity.Error, "the provider failed", "TST-70001")));

        var result = await CreateSut().Import("Shop", OptionsWithProvider(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the provider failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task ImportRefusesANameTheProviderResolvesToNoConnectionAndNamesIt()
    {
        _provider
            .Setup(p => p.Get("Shop", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(default!));

        var result = await CreateSut().Import("Shop", OptionsWithProvider(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("'Shop'");
        result.CurrentMessage.ShouldContain("resolves to no connection");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ImportAsksTheProviderForTheNameAsGivenAndReadsTheCatalogThroughTheConnectionItAnswers()
    {
        _provider
            .Setup(p => p.Get("Shop", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(_connection.Object));

        var result = await CreateSut().Import("Shop", OptionsWithProvider(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Name.ShouldBe("Shop");
        _provider.Verify(p => p.Get("Shop", It.IsAny<CancellationToken>()), Times.Once);
        _connection.Verify(
            c => c.Execute<IEnumerable<InformationSchemaSchemata>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Server=db;Database=Shop")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task ValidateRefusesTheSameSourcesAsImport(string source)
    {
        var result = await CreateSut().Validate(source, OptionsWithProvider(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        NeverAsksTheProvider();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateIsTrueWhenTheConnectionAnswersAndFailsWhenTheCatalogCannotBeRead()
    {
        _provider
            .Setup(p => p.Get("Shop", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(_connection.Object));
        var sut = CreateSut();

        (await sut.Validate("Shop", OptionsWithProvider(), TestContext.Current.CancellationToken)).Value.ShouldBeTrue();

        _connection
            .Setup(c => c.Execute<IEnumerable<InformationSchemaSchemata>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<InformationSchemaSchemata>>.Failure(new GenericMessage(MessageSeverity.Error, "the catalog read failed", "TST-70001")));

        var failed = await sut.Validate("Shop", OptionsWithProvider(), TestContext.Current.CancellationToken);

        failed.IsFailure.ShouldBeTrue();
        failed.CurrentMessage.ShouldNotBeNull().ShouldContain("the catalog read failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task TheInterfaceValidateCarriesNoOptionsSoItRefusesNamingTheMissingProviderInsteadOfOpeningAnything()
    {
        // Why: ISchemaImporter.Validate(source) has no options, so no provider; the overload with options is the one that can reach a connection.
        var result = await CreateSut().Validate("Shop", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("no connection provider");
        NeverAsksTheProvider();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AConnectionOfAnotherEngineFailsItsFirstCatalogReadAndTheImportFailsAndIsNeverAnEmptyStore()
    {
        // Why: IDataConnection declares no kind, so the importer does not check one; a connection of another engine cannot answer the SQL Server
        // catalog (sys.schemas and the like do not exist there), its first read fails, and that failure passes up. The import is a failure, not a
        // store with nothing in it.
        var otherEngine = new Mock<IDataConnection>();
        otherEngine
            .Setup(c => c.Execute<IEnumerable<InformationSchemaSchemata>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<InformationSchemaSchemata>>.Failure(new GenericMessage(MessageSeverity.Error, "the engine has no such catalog view", "TST-70001")));
        _provider
            .Setup(p => p.Get("Other", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(otherEngine.Object));

        var result = await CreateSut().Import("Other", OptionsWithProvider(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the engine has no such catalog view");
    }
}
