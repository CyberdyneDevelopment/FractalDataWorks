using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-3574: <see cref="ConnectionProvider"/>'s get-all answers every member its domain configuration provider
/// holds, each built through the implementation the member names. It answered an empty list before.
/// </summary>
public sealed class ConnectionProviderGetAllTests
{
    private const string Implementation = "GetAllTestImplementation";

    [Fact]
    public async Task GetAllAnswersEveryStoredMember()
    {
        var first = new Mock<IGenericConnection>().Object;
        var second = new Mock<IGenericConnection>().Object;
        var provider = ProviderOver(("first", first), ("second", second));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([first, second]);
    }

    [Fact]
    public async Task GetAllFailsWhenAMemberFailsToBuild()
    {
        var provider = ProviderOver(("first", new Mock<IGenericConnection>().Object), ("unbuildable", null));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task DataConnectionViewAnswersEveryStoredDataConnection()
    {
        var data = new Mock<IDataConnection>().Object;
        var document = new Mock<IDocumentConnection>().Object;
        var provider = ProviderOver(("data", data), ("document", document));

        var result = await ((IDataConnectionProvider)provider).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([data]);
    }

    // Each member is a configuration naming one implementation; a null service makes that member's
    // build fail, as an implementation provider that cannot build answers.
    private static ConnectionProvider ProviderOver(params (string Name, IGenericConnection? Service)[] members)
    {
        var configurations = members.Select(m =>
        {
            var configuration = new Mock<IConnectionImplementationConfiguration>();
            configuration.SetupProperty(c => c.Name, m.Name);
            configuration.SetupProperty(c => c.Implementation, Implementation);
            return configuration.Object;
        }).ToList();

        var store = new Mock<IDomainConfigurationProvider<IConnectionImplementationConfiguration>>();
        store.Setup(s => s.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IConnectionImplementationConfiguration>>.Success(configurations));

        var implementation = new Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>();
        implementation.Setup(i => i.Create(It.IsAny<IConnectionImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IConnectionImplementationConfiguration configuration, CancellationToken _) =>
                members.Single(m => string.Equals(m.Name, configuration.Name, StringComparison.Ordinal)).Service is { } service
                    ? GenericResult<IGenericConnection>.Success(service)
                    : GenericResult<IGenericConnection>.Failure(new Fdw.Messages.GenericMessage("cannot build")));

        var provider = new ConnectionProvider(NullLogger<ConnectionProvider>.Instance);
        provider.Register(store.Object).IsSuccess.ShouldBeTrue();
        provider.Register(Implementation, () => implementation.Object).IsSuccess.ShouldBeTrue();
        return provider;
    }
}
