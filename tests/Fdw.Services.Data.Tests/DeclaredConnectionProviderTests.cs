using System;
using System.Threading.Tasks;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A declared connection is built only from what the host declares in <c>configurationSchema.json</c> (FDW-1084):
/// a name it does not declare, a declaration with no kind, and a kind nobody registered are each a refusal, never a
/// lookup somewhere else.
/// </summary>
public sealed class DeclaredConnectionProviderTests
{
    private static DeclaredConnectionProvider Provider(params IConnectionImplementationConfiguration[] declared)
    {
        var schema = new ConfigurationSchema();
        foreach (var connection in declared)
            schema.Connections.Add(connection);

        return new DeclaredConnectionProvider(schema, new ServiceCollection().BuildServiceProvider());
    }

    private static IConnectionImplementationConfiguration Declared(string name, string implementation)
    {
        var connection = new Mock<IConnectionImplementationConfiguration>();
        connection.SetupGet(c => c.Name).Returns(name);
        connection.SetupGet(c => c.Implementation).Returns(implementation);
        return connection.Object;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AConnectionTheHostDoesNotDeclareIsRefused()
    {
        var result = await Provider(Declared("PlatformConfiguration", "MsSql"))
            .Get("AuthDb", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ADeclarationThatNamesNoKindIsRefused()
    {
        var result = await Provider(Declared("AuthDb", string.Empty))
            .Get("AuthDb", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AKindNobodyRegisteredIsRefused()
    {
        var result = await Provider(Declared("AuthDb", "NoSuchConnectionKind"))
            .Get("AuthDb", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructionRequiresTheSchemaAndTheServices()
    {
        Should.Throw<ArgumentNullException>(() => new DeclaredConnectionProvider(null!, new ServiceCollection().BuildServiceProvider()));
        Should.Throw<ArgumentNullException>(() => new DeclaredConnectionProvider(new ConfigurationSchema(), null!));
    }
}
