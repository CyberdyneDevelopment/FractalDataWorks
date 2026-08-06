using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SessionState;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.SessionState.Tests;

/// <summary>
/// Unit tests for <see cref="SessionStateConfigurationProvider"/>.
///
/// Only <see cref="IConfigurationGateway"/> is faked. The real provider runs under test.
/// </summary>
[Trait("Priority", "P1")]
public class SessionStateConfigurationProviderTests
{
    private static SessionStateConfigurationProvider MakeProvider(Mock<IConfigurationGateway> gateway)
        => new SessionStateConfigurationProvider(
            gateway.Object,
            NullLogger<SessionStateConfigurationProvider>.Instance);

    // ── GetRecord ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecord_WhenRowExists_ReturnsRecord()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        var expectedRecord = new SessionStateRecord { Id = Guid.NewGuid(), UserId = userId, StateKey = "ui:dashboard:filter" };

        gateway.Setup(g => g.Execute<IEnumerable<SessionStateRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SessionStateRecord>>.Success([expectedRecord]));

        var provider = MakeProvider(gateway);

        var result = await provider.GetRecord(userId, "ui:dashboard:filter", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.StateKey.ShouldBe("ui:dashboard:filter");
    }

    [Fact]
    public async Task GetRecord_WhenNoRow_ReturnsSuccessWithNull()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<SessionStateRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SessionStateRecord>>.Success([]));

        var provider = MakeProvider(gateway);

        var result = await provider.GetRecord(userId, "nonexistent", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetRecord_WhenGatewayFails_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<SessionStateRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SessionStateRecord>>.Failure(new GenericMessage("DB error")));

        var provider = MakeProvider(gateway);

        var result = await provider.GetRecord(userId, "any", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── GetAllRecords ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRecords_ReturnsAllUserRecords()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<SessionStateRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SessionStateRecord>>.Success([
                new SessionStateRecord { UserId = userId, StateKey = "k1" },
                new SessionStateRecord { UserId = userId, StateKey = "k2" }
            ]));

        var provider = MakeProvider(gateway);

        var result = await provider.GetAllRecords(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
    }

    // ── Insert ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Insert_WhenGatewaySucceeds_ReturnsSuccess()
    {
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var provider = MakeProvider(gateway);
        var record = new SessionStateRecord { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), StateKey = "k", StateValue = "v" };

        var result = await provider.Insert(record, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenGatewaySucceeds_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var provider = MakeProvider(gateway);

        var result = await provider.Delete(userId, "k", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    // ── DeleteAll ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAll_WhenGatewayFails_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("Delete failed")));

        var provider = MakeProvider(gateway);

        var result = await provider.DeleteAll(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
