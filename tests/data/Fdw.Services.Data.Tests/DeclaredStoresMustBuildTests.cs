using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A data store the host declares in configurationSchema.json that does not build stops the host with its coded messages, in one pass over every declared store,
/// and a gateway made by a direct <c>new</c> never answers an empty or partial tree (FDW-6433). Each case starts from a schema file on disk read by the loader, the way a
/// host reaches it, and reads the message's own code (not result.Code, FDW-6137).
/// </summary>
[Trait("Category", "DeclaredStores")]
public sealed class DeclaredStoresMustBuildTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger Log = NullLogger.Instance;

    private const string TwoStoresOfKindsNobodyRegistered
        = """{"ConfigurationSchema":{"DataStores":[{"Name":"First","Implementation":"NoSuchStoreTypeOne","Paths":[]},{"Name":"Second","Implementation":"NoSuchStoreTypeTwo","Paths":[]}]}}""";

    private static ConfigurationSchema Loaded(string schemaJson)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"schema-fdw6433-{Guid.NewGuid():N}.json");
        System.IO.File.WriteAllText(path, schemaJson);
        try
        {
            var loaded = ConfigurationSchemaLoader.Load(path, Log);
            loaded.IsSuccess.ShouldBeTrue(loaded.CurrentMessage);
            return loaded.Value!;
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AStoreOfAnImplementationNobodyRegisteredFailsTheBuildWithDATA61141NamingTheStore()
    {
        var schema = Loaded("""{"ConfigurationSchema":{"DataStores":[{"Name":"First","Implementation":"NoSuchStoreTypeOne","Paths":[]}]}}""");

        var tree = ConfigurationGateway.BuildFromSchema(schema, Log);

        tree.IsFailure.ShouldBeTrue();
        var refusal = tree.FirstFailureMessage().ShouldNotBeNull().Message;
        refusal.Code.ShouldBe("DATA-61141");
        refusal.Message.ShouldContain("First");
        refusal.Message.ShouldContain("NoSuchStoreTypeOne");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void EveryStoreThatDoesNotBuildIsReportedInOnePassNotOnlyTheFirst()
    {
        var tree = ConfigurationGateway.BuildFromSchema(Loaded(TwoStoresOfKindsNobodyRegistered), Log);

        tree.IsFailure.ShouldBeTrue();
        var refusals = tree.FailureMessages().Select(failure => failure.Message).ToList();
        refusals.Count.ShouldBe(2);
        refusals.ShouldAllBe(refusal => refusal.Code == "DATA-61141");
        refusals[0].Message.ShouldContain("First");
        refusals[1].Message.ShouldContain("Second");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AGatewayMadeByADirectNewNeverAnswersAnEmptyTreeForStoresThatDoNotBuild()
    {
        var gateway = new ConfigurationGateway(
            "PlatformConfiguration",
            new NoConnections(),
            Loaded(TwoStoresOfKindsNobodyRegistered),
            logger: null,
            cache: null,
            options: null,
            authenticationContextAccessor: null,
            publisher: null);

        var thrown = Should.Throw<InvalidOperationException>(() => gateway.DataStores);

        thrown.Message.ShouldContain("DATA-61141");
        thrown.Message.ShouldContain("First");
        thrown.Message.ShouldContain("Second");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASchemaThatDeclaresNoStoreBuildsAnEmptyTree()
    {
        var tree = ConfigurationGateway.BuildFromSchema(Loaded("""{"ConfigurationSchema":{}}"""), Log);

        tree.IsSuccess.ShouldBeTrue(tree.CurrentMessage);
        tree.Value.ShouldBeEmpty();
    }

    private sealed class NoConnections : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
    {
        public Task<IGenericResult<IGenericConnection>> Create(IConnectionImplementationConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<IGenericConnection>>(GenericResult<IGenericConnection>.Failure(new GenericMessage("no connection in this test")));

        public Task<IGenericResult<Fdw.Abstractions.IGenericService>> Create(IImplementationConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<Fdw.Abstractions.IGenericService>>(GenericResult<Fdw.Abstractions.IGenericService>.Failure(new GenericMessage("no connection in this test")));
    }
}
