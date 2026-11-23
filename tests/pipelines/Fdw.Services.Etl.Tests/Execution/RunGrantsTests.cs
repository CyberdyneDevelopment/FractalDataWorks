using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// The stored grants of runs (FDW-6144): the person is the caller and nobody a request states, a grant must allow something and end later than now, a grant that has ended or was revoked allows nothing, and a
/// grant is read back whole from the rows the configuration save path wrote.
/// </summary>
public sealed class RunGrantsTests
{
    private static readonly Guid Person = Guid.Parse("77777777-0000-0000-0000-000000000007");
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly List<RunGrantRecord> _grants = [];
    private readonly List<RunGrantPermissionRecord> _permissions = [];
    private readonly List<IDataCommand> _writes = [];
    private bool _readFails;

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private RunGrants Sut(bool caller = true, bool person = true)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<RunGrantRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _readFails
                ? GenericResult<IEnumerable<RunGrantRecord>>.Failure(Mock.Of<IGenericMessage>(m => m.Message == "the store is down"))
                : GenericResult<IEnumerable<RunGrantRecord>>.Success(_grants.Where(row => row.IsCurrent).ToList()));
        gateway
            .Setup(g => g.Execute<IEnumerable<RunGrantPermissionRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IEnumerable<RunGrantPermissionRecord>>.Success(_permissions.Where(row => row.IsCurrent && !row.IsDeleted).ToList()));
        gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) =>
            {
                _writes.Add(command);
                if (command is ConfigurationSaveCommand<RunGrantRecord> grant && grant.Data is { } grantRow)
                {
                    _grants.Add(grantRow);
                }
                else if (command is ConfigurationSaveCommand<RunGrantPermissionRecord> permission && permission.Data is { } permissionRow)
                {
                    _permissions.Add(permissionRow);
                }
                else if (command is ConfigurationRetireCommand retire)
                {
                    foreach (var row in _grants.Where(row => row.Id.Equals(retire.KeyValues[0])))
                    {
                        row.IsDeleted = true;
                    }
                }

                return GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>()));
            });
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(person);
        context.SetupGet(c => c.IsSystemContext).Returns(false);
        context.SetupGet(c => c.UserId).Returns(Person.ToString("D"));
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(caller ? context.Object : null);
        return new RunGrants(gateways.Object, accessor.Object, new FixedClock(Now), NullLogger<RunGrants>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheCallerIsThePersonOfTheGrantAndNothingElseCanStateOne()
    {
        var issued = await Sut().Issue(["pipelines:execute"], Now.AddDays(7), TestContext.Current.CancellationToken);

        issued.IsSuccess.ShouldBeTrue(issued.CurrentMessage);
        issued.Value.ShouldNotBeNull().PersonId.ShouldBe(Person);
        _grants.ShouldHaveSingleItem().PersonId.ShouldBe(Person);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerThatIsNoPersonCannotGiveAGrantAndNothingIsWritten()
    {
        (await Sut(caller: false).Issue(["pipelines:execute"], Now.AddDays(7), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut(person: false).Issue(["pipelines:execute"], Now.AddDays(7), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AGrantThatEndsNowOrBeforeIsRefusedAndNothingIsWritten()
    {
        (await Sut().Issue(["pipelines:execute"], Now, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut().Issue(["pipelines:execute"], Now.AddSeconds(-1), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AGrantThatAllowsNothingIsRefusedAndNothingIsWritten()
    {
        (await Sut().Issue([], Now.AddDays(1), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut().Issue([" ", string.Empty], Now.AddDays(1), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task IssuingWritesEachPermissionOnceAndThenTheGrantThroughTheConfigurationSavePath()
    {
        var issued = await Sut().Issue(["pipelines:execute", "PIPELINES:EXECUTE", " data:read "], Now.AddDays(1), TestContext.Current.CancellationToken);

        issued.IsSuccess.ShouldBeTrue(issued.CurrentMessage);
        issued.Value.ShouldNotBeNull().Permissions.ShouldBe(["pipelines:execute", "data:read"]);
        _writes.Count.ShouldBe(3);
        _writes[0].ShouldBeOfType<ConfigurationSaveCommand<RunGrantPermissionRecord>>();
        _writes[1].ShouldBeOfType<ConfigurationSaveCommand<RunGrantPermissionRecord>>();
        _writes[2].ShouldBeOfType<ConfigurationSaveCommand<RunGrantRecord>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AGrantIsReadBackWithItsPermissionsAndEndsWhenItsTimeComes()
    {
        var sut = Sut();
        var issued = await sut.Issue(["pipelines:execute", "data:read"], Now.AddDays(1), TestContext.Current.CancellationToken);

        var read = await sut.Read(issued.Value!.Id, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var grant = read.Value.ShouldNotBeNull();
        grant.Permissions.ShouldBe(["pipelines:execute", "data:read"], ignoreOrder: true);
        grant.HasEnded(Now.AddHours(23)).ShouldBeFalse();
        grant.HasEnded(Now.AddDays(1)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARevokedGrantIsStillReadAndHasEnded()
    {
        var sut = Sut();
        var issued = await sut.Issue(["pipelines:execute"], Now.AddDays(1), TestContext.Current.CancellationToken);

        (await sut.Revoke(issued.Value!.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writes.Last().ShouldBeOfType<ConfigurationRetireCommand>();
        var read = await sut.Read(issued.Value.Id, TestContext.Current.CancellationToken);
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value.ShouldNotBeNull().IsRevoked.ShouldBeTrue();
        read.Value.HasEnded(Now).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AGrantThatIsNotThereIsNotFoundAndRevokingItWritesNothing()
    {
        var sut = Sut();

        (await sut.Read(Guid.NewGuid(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await sut.Revoke(Guid.NewGuid(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantThatCannotBeReadIsAFailureAndNeverAnAnswer()
    {
        _readFails = true;

        (await Sut().Read(Guid.NewGuid(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AGrantNamesWhatARunWouldHoldThatItDoesNotAllow()
    {
        var grant = new RunGrant { Permissions = ["pipelines:execute", "data:read"] };

        grant.NotAllowed(["DATA:READ", "pipelines:execute"]).ShouldBeEmpty();
        grant.NotAllowed(["data:write", "data:write", "data:read"]).ShouldBe(["data:write"]);
    }
}
