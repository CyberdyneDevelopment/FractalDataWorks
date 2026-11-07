using Fdw.Results;
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
/// FDW-3951: <see cref="ConnectionProvider"/> refuses a disabled connection with the coded failure ConnectionDisabled before it
/// asks a factory for anything, however the connection is asked for, and builds an enabled one as before.
/// </summary>
public sealed class ConnectionProviderDisabledTests
{
    private const string Implementation = "DisabledTestImplementation";
    private static readonly Guid DisabledId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EnabledId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ADisabledConnectionAskedForByNameIsRefusedAndNoFactoryIsAsked()
    {
        var fixture = new Fixture(new DisabledStatesDouble().Disabled(DisabledId, "three failed runs in a row"));

        var result = await fixture.Provider.Get("disabled", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("ConnectionDisabled");
        fixture.Built.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ADisabledConnectionAskedForByIdIsRefusedAndNoFactoryIsAsked()
    {
        var fixture = new Fixture(new DisabledStatesDouble().Disabled(DisabledId, "three failed runs in a row"));

        var result = await fixture.Provider.Get(DisabledId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("ConnectionDisabled");
        fixture.Built.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ARefusedConnectionNamesTheConnectionWhenItWasDisabledAndWhy()
    {
        var at = new DateTimeOffset(2026, 11, 4, 12, 30, 0, TimeSpan.Zero);
        var fixture = new Fixture(new DisabledStatesDouble().Disabled(DisabledId, "three failed runs in a row", at));

        var result = await fixture.Provider.Get("disabled", TestContext.Current.CancellationToken);

        var message = result.Code!.FormatMessage(result.Details);
        message.ShouldContain("disabled");
        message.ShouldContain("three failed runs in a row");
        message.ShouldContain("2026-11-04T12:30:00");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionNobodyDisabledIsBuiltAsBefore()
    {
        var fixture = new Fixture(new DisabledStatesDouble().Disabled(DisabledId, "three failed runs in a row"));

        var result = await fixture.Provider.Get("enabled", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Built.ShouldBe(["enabled"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionWhoseStateCannotBeReadIsNotHandedOut()
    {
        var unreadable = GenericResult.Failure(new Fdw.Messages.GenericMessage("the store did not answer"));
        var fixture = new Fixture(new DisabledStatesDouble().ReadFails(unreadable));

        var result = await fixture.Provider.Get("enabled", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        fixture.Built.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheListOfEveryConnectionLeavesADisabledOneOutInsteadOfFailing()
    {
        var fixture = new Fixture(new DisabledStatesDouble().Disabled(DisabledId, "three failed runs in a row"));

        var result = await fixture.Provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Built.ShouldBe(["enabled"]);
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AConfigurationHandedInIsRefusedLikeOneRead()
    {
        var fixture = new Fixture(new DisabledStatesDouble().Disabled(DisabledId, "three failed runs in a row"));

        var result = await fixture.Provider.Get(fixture.Configurations.Single(c => c.Id == DisabledId), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("ConnectionDisabled");
    }

    // A provider over two stored connections, one of which the test may disable. Each build is recorded by name.
    private sealed class Fixture
    {
        public Fixture(DisabledStatesDouble states)
        {
            Configurations =
            [
                Configuration(DisabledId, "disabled"),
                Configuration(EnabledId, "enabled"),
            ];

            var store = new Mock<IDomainConfigurationProvider<IConnectionImplementationConfiguration>>();
            store.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string name, CancellationToken _) => GenericResult<IConnectionImplementationConfiguration>.Success(
                    Configurations.Single(c => string.Equals(c.Name, name, StringComparison.Ordinal))));
            store.Setup(s => s.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => GenericResult<IConnectionImplementationConfiguration>.Success(
                    Configurations.Single(c => c.Id == id)));
            store.Setup(s => s.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<IConnectionImplementationConfiguration>>.Success(Configurations));

            var implementation = new Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>();
            implementation.Setup(i => i.Create(It.IsAny<IConnectionImplementationConfiguration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IConnectionImplementationConfiguration configuration, CancellationToken _) =>
                {
                    Built.Add(configuration.Name);
                    return GenericResult<IGenericConnection>.Success(new Mock<IGenericConnection>().Object);
                });

            Provider = new ConnectionProvider(states, NullLogger<ConnectionProvider>.Instance);
            Provider.Register(store.Object).IsSuccess.ShouldBeTrue();
            Provider.Register(Implementation, () => implementation.Object).IsSuccess.ShouldBeTrue();
        }

        public ConnectionProvider Provider { get; }

        public List<IConnectionImplementationConfiguration> Configurations { get; }

        public List<string> Built { get; } = [];

        private static IConnectionImplementationConfiguration Configuration(Guid id, string name)
        {
            var configuration = new Mock<IConnectionImplementationConfiguration>();
            configuration.SetupProperty(c => c.Id, id);
            configuration.SetupProperty(c => c.Name, name);
            configuration.SetupProperty(c => c.Implementation, Implementation);
            return configuration.Object;
        }
    }
}
