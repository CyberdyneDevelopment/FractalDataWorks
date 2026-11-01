using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.SignIns;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.SignIns;

/// <summary>
/// FDW-2729: a sign-in is one new row in AuthDb, written in the system scope; the last sign-ins are read from the view for the people asked about.
/// </summary>
public sealed class DatabaseSignInLogTests
{
    private static readonly Guid Person = Guid.Parse("0a1b2c3d-0000-0000-0000-00000000000a");
    private static readonly Guid Other = Guid.Parse("0a1b2c3d-0000-0000-0000-00000000000b");
    private static readonly DateTimeOffset At = new(2026, 10, 24, 13, 30, 0, TimeSpan.Zero);

    private sealed class FixedCaller(SignInCaller caller) : ISignInCaller
    {
        public SignInCaller Current() => caller;
    }

    private sealed class Store
    {
        public AuthenticationContextAccessor Accessor { get; } = new();

        public SignInCaller Caller { get; set; } = SignInCaller.NotKnown;

        public IGenericResult<IEnumerable<UserSignInEntity>> Listed { get; set; } = GenericResult<IEnumerable<UserSignInEntity>>.Success([]);

        public List<(IDataCommand Command, DataStoreTarget Target)> Calls { get; } = [];

        public List<bool> SystemWhenCalled { get; } = [];

        public List<bool> CacheUsed { get; } = [];

        public DatabaseSignInLog Build(IGenericResult<int> written, IGenericResult<IEnumerable<UserLastSignInEntity>> read)
        {
            var gateway = new Mock<IConfigurationGateway>();
            gateway
                .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
                {
                    SystemWhenCalled.Add(Accessor.Current?.IsSystemContext == true);
                    Calls.Add((command, target));
                    return Task.FromResult(written);
                });
            gateway
                .Setup(g => g.Execute<IEnumerable<UserLastSignInEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken _) =>
                {
                    SystemWhenCalled.Add(Accessor.Current?.IsSystemContext == true);
                    CacheUsed.Add(useCache);
                    Calls.Add((command, target));
                    return Task.FromResult(read);
                });
            gateway
                .Setup(g => g.Execute<IEnumerable<UserSignInEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken _) =>
                {
                    SystemWhenCalled.Add(Accessor.Current?.IsSystemContext == true);
                    CacheUsed.Add(useCache);
                    Calls.Add((command, target));
                    return Task.FromResult(Listed);
                });
            var gateways = new Mock<IConfigurationGatewayProvider>();
            gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
            return new DatabaseSignInLog(gateways.Object, Accessor, new FixedCaller(Caller), NullLogger<DatabaseSignInLog>.Instance);
        }

        public DatabaseSignInLog Build(IGenericResult<int> written)
            => Build(written, GenericResult<IEnumerable<UserLastSignInEntity>>.Success([]));

        public DatabaseSignInLog Build(IGenericResult<IEnumerable<UserLastSignInEntity>> read)
            => Build(GenericResult<int>.Success(1), read);
    }

    private static IEnumerable<FilterCondition> Conditions(IFilterNode? node) => node switch
    {
        FilterCondition condition => [condition],
        FilterGroup group => group.Nodes.SelectMany(Conditions),
        _ => [],
    };

    [Fact]
    public async Task ASignInIsOneNewRowInAuthDbWrittenInTheSystemScope()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<int>.Success(1))
            .Record(Person, "Console", "password-session", At, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var (command, target) = store.Calls.ShouldHaveSingleItem();
        target.Container.ShouldBe("UserSignIn");
        target.Path.ShouldBe("auth");
        target.DataStore.ShouldBe(AuthenticationStepTypes.StateConnection);
        var row = command.ShouldBeOfType<InsertCommand<UserSignInEntity>>().Data.ShouldHaveSingleItem();
        row.UserId.ShouldBe(Person);
        row.SignedInAt.ShouldBe(At);
        row.ServiceName.ShouldBe("Console");
        row.FlowName.ShouldBe("password-session");
        row.Id.ShouldNotBe(Guid.Empty);
        store.SystemWhenCalled.ShouldAllBe(system => system);
        store.Accessor.Current.ShouldBeNull();
    }

    [Fact]
    public async Task ASignInThatIssuedABearerTokenHasNoServiceName()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<int>.Success(1))
            .Record(Person, null, "token-exchange", At, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = store.Calls.ShouldHaveSingleItem().Command.ShouldBeOfType<InsertCommand<UserSignInEntity>>().Data.ShouldHaveSingleItem();
        row.ServiceName.ShouldBeNull();
    }

    [Fact]
    public async Task AWriteThatFailsIsReportedAsAFailure()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<int>.Failure(new GenericMessage("the database refused the row")))
            .Record(Person, "Console", "password-session", At, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task NobodyAskedAboutReadsNothing()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<int>.Success(1)).LastSignIns([], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ShouldBeEmpty();
        store.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheLastSignInOfEachPersonAskedAboutIsReadFromTheViewInTheSystemScope()
    {
        var store = new Store();
        var rows = new[] { new UserLastSignInEntity { UserId = Person, LastSignInAt = At } };

        var result = await store.Build(GenericResult<IEnumerable<UserLastSignInEntity>>.Success(rows))
            .LastSignIns([Person, Other], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value![Person].ShouldBe(At);
        result.Value.ContainsKey(Other).ShouldBeFalse();
        var (command, target) = store.Calls.ShouldHaveSingleItem();
        target.Container.ShouldBe("UserLastSignIn");
        target.Path.ShouldBe("auth");
        var condition = Conditions(command.ShouldBeOfType<QueryCommand<UserLastSignInEntity>>().Filter?.Root).ShouldHaveSingleItem();
        condition.PropertyName.ShouldBe("UserId");
        ((System.Collections.IEnumerable)condition.Value!).Cast<object>().Select(v => Guid.Parse(v.ToString()!)).ShouldBe([Person, Other], ignoreOrder: true);
        store.SystemWhenCalled.ShouldAllBe(system => system);
    }

    [Fact]
    public async Task TheLastSignInIsReadWithoutTheResultCache()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<IEnumerable<UserLastSignInEntity>>.Success([]))
            .LastSignIns([Person], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        store.CacheUsed.ShouldHaveSingleItem().ShouldBeFalse();
    }

    [Fact]
    public async Task AReadThatFailsIsReportedAsAFailureAndNotAsNeverSignedIn()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<IEnumerable<UserLastSignInEntity>>.Failure(new GenericMessage("the view could not be read")))
            .LastSignIns([Person], TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    private const string EdgeOnWindows =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0";

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task ASignInRecordsWhoWasCallingAndNamesTheirBrowserAndSystem()
    {
        var store = new Store { Caller = new SignInCaller { IpAddress = "203.0.113.9", UserAgent = EdgeOnWindows } };

        var result = await store.Build(GenericResult<int>.Success(1))
            .Record(Person, "Console", "password-session", At, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = store.Calls.ShouldHaveSingleItem().Command.ShouldBeOfType<InsertCommand<UserSignInEntity>>().Data.ShouldHaveSingleItem();
        row.IpAddress.ShouldBe("203.0.113.9");
        row.UserAgent.ShouldBe(EdgeOnWindows);
        row.Browser.ShouldBe("Edge");
        row.OperatingSystem.ShouldBe("Windows");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task ASignInFromACallerThatIsNotKnownStoresNothingForTheCaller()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<int>.Success(1))
            .Record(Person, null, "agent key", At, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = store.Calls.ShouldHaveSingleItem().Command.ShouldBeOfType<InsertCommand<UserSignInEntity>>().Data.ShouldHaveSingleItem();
        row.IpAddress.ShouldBeNull();
        row.UserAgent.ShouldBeNull();
        row.Browser.ShouldBeNull();
        row.OperatingSystem.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task AUserAgentThatNamesNoKnownBrowserKeepsTheRawStringAndNamesNothing()
    {
        var store = new Store { Caller = new SignInCaller { IpAddress = "198.51.100.4", UserAgent = "curl/8.5.0" } };

        var result = await store.Build(GenericResult<int>.Success(1))
            .Record(Person, null, "personal access token", At, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = store.Calls.ShouldHaveSingleItem().Command.ShouldBeOfType<InsertCommand<UserSignInEntity>>().Data.ShouldHaveSingleItem();
        row.UserAgent.ShouldBe("curl/8.5.0");
        row.Browser.ShouldBeNull();
        row.OperatingSystem.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task APeopleSignInsAreReadNewestFirstAPageAtATimeFromTheTableWithoutTheResultCache()
    {
        var store = new Store
        {
            Listed = GenericResult<IEnumerable<UserSignInEntity>>.Success(
            [
                new UserSignInEntity
                {
                    Id = Guid.NewGuid(), UserId = Person, SignedInAt = At, ServiceName = "Console", FlowName = "password-session",
                    IpAddress = "203.0.113.9", UserAgent = EdgeOnWindows, Browser = "Edge", OperatingSystem = "Windows",
                },
            ]),
        };

        var result = await store.Build(GenericResult<int>.Success(1)).SignIns(Person, 20, 11, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var record = result.Value!.ShouldHaveSingleItem();
        record.SignedInAt.ShouldBe(At);
        record.ServiceName.ShouldBe("Console");
        record.FlowName.ShouldBe("password-session");
        record.IpAddress.ShouldBe("203.0.113.9");
        record.UserAgent.ShouldBe(EdgeOnWindows);
        record.Browser.ShouldBe("Edge");
        record.OperatingSystem.ShouldBe("Windows");
        var (command, target) = store.Calls.ShouldHaveSingleItem();
        target.Container.ShouldBe("UserSignIn");
        target.Path.ShouldBe("auth");
        var query = command.ShouldBeOfType<QueryCommand<UserSignInEntity>>();
        var condition = Conditions(query.Filter?.Root).ShouldHaveSingleItem();
        condition.PropertyName.ShouldBe("UserId");
        Guid.Parse(condition.Value!.ToString()!).ShouldBe(Person);
        var ordered = query.Ordering!.OrderedFields.ShouldHaveSingleItem();
        ordered.PropertyName.ShouldBe("SignedInAt");
        ordered.Direction.Name.ShouldBe("Descending");
        query.Paging!.Skip.ShouldBe(20);
        query.Paging.Take.ShouldBe(11);
        store.CacheUsed.ShouldHaveSingleItem().ShouldBeFalse();
        store.SystemWhenCalled.ShouldAllBe(system => system);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task APersonWhoNeverSignedInHasAnEmptyList()
    {
        var store = new Store();

        var result = await store.Build(GenericResult<int>.Success(1)).SignIns(Person, 0, 10, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task AListThatCannotBeReadIsAFailureAndNotAnEmptyList()
    {
        var store = new Store { Listed = GenericResult<IEnumerable<UserSignInEntity>>.Failure(new GenericMessage("the table could not be read")) };

        var result = await store.Build(GenericResult<int>.Success(1)).SignIns(Person, 0, 10, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
}
