using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1003: a page of data stores names its connections from the connection domain's own table, in ONE
/// query for the whole page, after ONE check of whether the caller may read connections.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Configuration")]
public class ConnectionNamesTests
{
    private const string ReadPolicy = "connections:read";

    private static readonly ClaimsPrincipal Caller = new();

    private static DomainConfiguration Connection(string name)
        => new() { Id = Guid.NewGuid(), Name = name, Domain = "Connection", Implementation = "MsSql" };

    private static Mock<IAuthorizationService> Authorization(bool allowed)
    {
        var authorization = new Mock<IAuthorizationService>();
        authorization
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), ReadPolicy))
            .ReturnsAsync(allowed ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        return authorization;
    }

    // The connections the store holds; a read by ids answers those of them the caller names.
    private static Mock<IDomainRecordProvider<DomainConfiguration>> Connections(params DomainConfiguration[] held)
    {
        var connections = new Mock<IDomainRecordProvider<DomainConfiguration>>();
        connections
            .Setup(c => c.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                GenericResult<IReadOnlyList<DomainConfiguration>>.Success(held.Where(connection => ids.Contains(connection.Id)).ToList()));
        return connections;
    }

    [Fact]
    public async Task EveryConnectionAPageNamesIsNamedFromOneQueryAfterOnePolicyCheck()
    {
        var operations = Connection("OpsDb");
        var authentication = Connection("AuthDb");
        var connections = Connections(operations, authentication, Connection("Unrelated"));
        var authorization = Authorization(true);
        var sut = new ConnectionNames(connections.Object, authorization.Object, NullLogger.Instance);

        var read = await sut.Read(new[] { operations.Id, authentication.Id, operations.Id }, Caller, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Of(operations.Id, "OpsStore").ShouldBe("OpsDb");
        read.Value.Of(authentication.Id, "AuthStore").ShouldBe("AuthDb");
        read.Value.Of(operations.Id, "SecondOpsStore").ShouldBe("OpsDb");

        // Naming a row asks nothing more of either: the policy was one check and the read one query.
        authorization.Verify(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), ReadPolicy), Times.Once);
        connections.Verify(c => c.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ACallerWhoMayNotReadConnectionsGetsNoNamesAndNoConnectionIsRead()
    {
        var operations = Connection("OpsDb");
        var connections = Connections(operations);
        var sut = new ConnectionNames(connections.Object, Authorization(false).Object, NullLogger.Instance);

        var read = await sut.Read(new[] { operations.Id }, Caller, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Of(operations.Id, "OpsStore").ShouldBeNull();
        connections.Verify(c => c.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AConnectionTheCallerCannotSeeHasNoName()
    {
        // Why: row-level security hides the row, so the read does not return it; that is an absence the
        // response declares (no connection name), not a failure.
        var visible = Connection("OpsDb");
        var hidden = Guid.NewGuid();
        var sut = new ConnectionNames(Connections(visible).Object, Authorization(true).Object, NullLogger.Instance);

        var read = await sut.Read(new[] { visible.Id, hidden }, Caller, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Of(visible.Id, "OpsStore").ShouldBe("OpsDb");
        read.Value.Of(hidden, "HiddenStore").ShouldBeNull();
    }

    [Fact]
    public async Task AConnectionReadThatFailsFailsTheReadInsteadOfNamingNothing()
    {
        // Why: "the caller may not see it" and "the connections could not be read" are different facts, and an
        // empty name reported for the second would be a silent outage.
        var connections = new Mock<IDomainRecordProvider<DomainConfiguration>>();
        connections
            .Setup(c => c.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DomainConfiguration>>.Failure(new GenericMessage("connection store unavailable")));
        var sut = new ConnectionNames(connections.Object, Authorization(true).Object, NullLogger.Instance);

        var read = await sut.Read(new[] { Guid.NewGuid() }, Caller, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeFalse();
    }
}
