using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Limits;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Limits;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// A connection's description lives in the descriptions table and not on <c>conn.Connection</c> (FDW-6208): the provider describes the connection in the unit of its
/// save, fills the text once for every set of composed connections it reads (a read by id, by name and a list are each one such set) and removes it with the connection.
/// The member is on <see cref="IConnectionImplementationConfiguration"/>, so one observer serves every connection type. The writer and reader are strict stand-ins;
/// <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class ConnectionDescriptionsTests
{
    private static readonly Guid Warehouse = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid Archive = Guid.Parse("00000000-0000-0000-0000-0000000000d2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private ConnectionConfigurationProvider Connections()
        => new(
            NullLogger<ConnectionConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "conn", "Connection",
            _writer.Object, _reader.Object);

    // The reader answers with the texts it holds, put on each connection through the provider's own Id and fill delegates.
    private void TheReaderHolds(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill(
                "Connection", "Text", It.IsAny<IReadOnlyList<IConnectionImplementationConfiguration>>(), It.IsAny<Func<IConnectionImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IConnectionImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IConnectionImplementationConfiguration>, Func<IConnectionImplementationConfiguration, Guid>, Action<IConnectionImplementationConfiguration, string>, CancellationToken>(
                (_, _, things, idOf, fill, _) =>
                {
                    foreach (var thing in things)
                    {
                        if (texts.TryGetValue(idOf(thing), out var text)) fill(thing, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var connections = Connections();

        connections.ShouldBeAssignableTo<IConfigurationWriteObserver<IConnectionImplementationConfiguration>>();
        connections.ShouldBeAssignableTo<IConfigurationReadObserver<IConnectionImplementationConfiguration>>();
        connections.ShouldBeAssignableTo<IReferencedConfigurationProvider<IConnectionImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRecordsMappersHaveNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        new MsSqlConnectionConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
        new MsSqlConnectionLimitConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
        new HttpConnectionLimitConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("The warehouse database the order loads read from.")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedConnectionIsDescribedUnderTheConnectionTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Connection", "Text", Warehouse, "warehouse", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Connections().Written(
            new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Connection", "Text", Warehouse, "warehouse", "Warehouse", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Connections().Written(
            new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse", Description = "Warehouse" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedConnectionAndOneWithNoneStaysEmpty()
    {
        var connections = new List<IConnectionImplementationConfiguration>
        {
            new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse" },
            new MsSqlConnectionConfiguration { Id = Archive, Name = "archive" },
        };
        TheReaderHolds(new Dictionary<Guid, string> { [Warehouse] = "The warehouse database" });

        var read = await Connections().Read(connections, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        connections[0].Description.ShouldBe("The warehouse database");
        connections[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadOfOneConnectionByIdOrByNameIsAFillOfASetOfOne()
    {
        var byId = new List<IConnectionImplementationConfiguration> { new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse" } };
        var byName = new List<IConnectionImplementationConfiguration> { new MsSqlConnectionConfiguration { Id = Archive, Name = "archive" } };
        TheReaderHolds(new Dictionary<Guid, string> { [Warehouse] = "The warehouse database", [Archive] = "Closed years" });

        (await Connections().Read(byId, null, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await Connections().Read(byName, null, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        byId[0].Description.ShouldBe("The warehouse database");
        byName[0].Description.ShouldBe("Closed years");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheRead()
    {
        _reader
            .Setup(r => r.Fill(
                "Connection", "Text", It.IsAny<IReadOnlyList<IConnectionImplementationConfiguration>>(), It.IsAny<Func<IConnectionImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IConnectionImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Connections().Read([new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse" }], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachReadAndWritePassesTheCallersToken()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill(
                "Connection", "Text", It.IsAny<IReadOnlyList<IConnectionImplementationConfiguration>>(), It.IsAny<Func<IConnectionImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IConnectionImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.Describe("Connection", "Text", Warehouse, "warehouse", "Warehouse", It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteAbout("Connection", Warehouse, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());

        await Connections().Read([new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse" }], null, cancelled.Token);
        await Connections().Written(new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse", Description = "Warehouse" }, null!, cancelled.Token);
        await Connections().RemoveReferences(Warehouse, null!, cancelled.Token);

        _reader.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAConnectionRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Connection", Warehouse, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Connections().RemoveReferences(Warehouse, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
