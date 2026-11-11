using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-3692: a new thing is placed in the default workspace of its tenant's default organization with one <c>Owns</c> row, inserted under the
/// workspace's current configuration row inside the unit that makes the thing; every missing piece (no tenant, no user, no default organization,
/// no default workspace, a kind that is not a workspace resource kind) is a refusal that names it and writes nothing.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class DefaultWorkspacePlacementTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid Organization = Guid.NewGuid();
    private static readonly Guid DefaultWorkspace = Guid.NewGuid();
    private const string Store = "PlatformConfiguration";

    private sealed class Accessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private sealed class Rig
    {
        public Mock<IDataGatewayTransaction> Transaction { get; } = new();

        public Accessor Authentication { get; } = new();

        public Mock<IWorkspaceFoundingPolicy> Policy { get; } = new();

        public List<IDataCommand> Inserts { get; } = [];

        public Rig(bool configurationRow = true)
        {
            var caller = new Mock<IAuthenticationContext>();
            caller.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
            caller.SetupGet(c => c.UserId).Returns(User.ToString("D"));
            Authentication.Current = caller.Object;
            Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Success(true));

            DefaultOrganizations([new DefaultOrganizationRow { Id = Organization, Name = "Default Organization", DefaultWorkspaceId = DefaultWorkspace }]);

            IEnumerable<Dictionary<string, object?>> parent = configurationRow
                ? [new Dictionary<string, object?>(StringComparer.Ordinal) { ["RowId"] = 7, ["Id"] = DefaultWorkspace }]
                : [];
            Transaction.Setup(t => t.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(parent));
            Transaction.Setup(t => t.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Callback((IDataCommand command, DataStoreTarget target, CancellationToken token) => Inserts.Add(command))
                .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null)));
        }

        // What tenant.Organization holds as the tenant's default organization: the placement reads it in the unit, not through a provider.
        public void DefaultOrganizations(IEnumerable<DefaultOrganizationRow> rows) =>
            Transaction.Setup(t => t.Execute<IEnumerable<DefaultOrganizationRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IEnumerable<DefaultOrganizationRow>>.Success(rows));

        public DefaultWorkspacePlacement Placement() =>
            new(Authentication, Policy.Object, Store, "workspace", "tenant", NullLogger<DefaultWorkspacePlacement>.Instance);

        public async Task<ConfigurationWriteScope> Unit()
        {
            var gateway = new Mock<IConfigurationGateway>();
            gateway.SetupGet(g => g.DataStores).Returns([WorkspaceStore()]);
            gateway.Setup(g => g.BeginTransaction(Store, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataGatewayTransaction>.Success(Transaction.Object));
            var begun = await ConfigurationWriteScope.Begin(gateway.Object, Store, NullLogger.Instance, TestContext.Current.CancellationToken);
            return begun.Value ?? throw new InvalidOperationException("the test unit could not be opened");
        }
    }

    // workspace.WorkspaceConfiguration owns workspace.WorkspaceResource through WorkspaceConfigurationRowId, which references its RowId.
    private static IDataStore WorkspaceStore()
    {
        var resource = Container("WorkspaceResource", [], [Key("Physical", "RowId"), Key("Foreign", "WorkspaceConfigurationRowId")]);
        var referenced = new Mock<IDataField>();
        referenced.Setup(f => f.Name).Returns("RowId");
        var owns = new ReferencingKeyBinding(Key("Foreign", "WorkspaceConfigurationRowId", referenced.Object), resource);
        var configuration = Container("WorkspaceConfiguration", [owns], [Key("Physical", "RowId"), Key("Logical", "Id")]);
        IReadOnlyList<IDataContainer> containers = [configuration, resource];

        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Name).Returns("workspace");
        path.Setup(p => p.Containers).Returns(containers);
        path.Setup(p => p.Container(It.IsAny<string>())).Returns((string name) =>
            containers.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal)) is { } found
                ? GenericResult<IDataContainer>.Success(found)
                : GenericResult<IDataContainer>.Failure(new GenericMessage($"No container '{name}'")));
        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns(Store);
        store.Setup(s => s.Path(It.IsAny<string>())).Returns(GenericResult<IDataNodePath>.Success(path.Object));
        return store.Object;
    }

    private static IDataContainer Container(string name, IReadOnlyList<ReferencingKeyBinding> referencing, IReadOnlyList<IContainerKey> keys)
    {
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Keys).Returns(keys);
        container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
        return container.Object;
    }

    private static IContainerKey Key(string keyType, string column, IDataField? referenced = null)
    {
        var local = new Mock<IDataField>();
        local.Setup(f => f.Name).Returns(column);
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(local.Object);
        keyField.Setup(k => k.ReferencedField).Returns(referenced);
        KeyTypeBase type = keyType switch
        {
            "Physical" => new PhysicalKeyType(),
            "Logical" => new LogicalKeyType(),
            "Foreign" => new ForeignKeyType(),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "unsupported key type in test"),
        };
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyType).Returns(type);
        key.Setup(k => k.KeyName).Returns($"{keyType}_{column}");
        key.Setup(k => k.KeyFields).Returns([keyField.Object]);
        return key.Object;
    }

    private static PlacementRequest Request(string kind = "DataSet") => new(kind, Guid.NewGuid(), "NflTeams");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task ANewThingGetsAnOwnsRowUnderTheDefaultWorkspacesCurrentConfiguration()
    {
        var rig = new Rig();
        var request = Request();

        var placed = await rig.Placement().Place(request, await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue(placed.CurrentMessage);
        var insert = rig.Inserts.ShouldHaveSingleItem().ShouldBeAssignableTo<IConfigurationSaveCommand>();
        var row = insert.InputData.ShouldBeOfType<WorkspaceResourceConfiguration>();
        row.ResourceType.ShouldBe("DataSet");
        row.ResourceId.ShouldBe(request.Id);
        row.Name.ShouldBe("NflTeams");
        row.Relationship.ShouldBe("Owns");
        row.AddedByUserId.ShouldBe(User);
        insert.FieldValues["WorkspaceConfigurationRowId"].ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task EveryWorkspaceResourceKindCanBePlaced()
    {
        var kinds = WorkspaceResourceKinds.All().Select(kind => kind.Name).ToList();
        kinds.ShouldNotBeEmpty();

        foreach (var kind in kinds)
        {
            var rig = new Rig();

            var placed = await rig.Placement().Place(Request(kind), await rig.Unit(), TestContext.Current.CancellationToken);

            placed.IsSuccess.ShouldBeTrue($"{kind}: {placed.CurrentMessage}");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AKindThatIsNotAWorkspaceResourceKindIsRefusedAndNothingIsWritten()
    {
        var rig = new Rig();

        var placed = await rig.Placement().Place(Request("Sandwich"), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Code.ShouldBeOfType<PlacementKindUnknownCode>();
        rig.Inserts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task ACallerActingInNoTenantIsRefusedAndNothingIsWritten()
    {
        var rig = new Rig();
        rig.Authentication.Current = null;

        var placed = await rig.Placement().Place(Request(), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Code.ShouldBeOfType<PlacementTenantUnknownCode>();
        rig.Inserts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task ACallerWhoseIdIsNotAGuidIsRefusedAndNothingIsWritten()
    {
        var rig = new Rig();
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        caller.SetupGet(c => c.UserId).Returns("a-service");
        rig.Authentication.Current = caller.Object;

        var placed = await rig.Placement().Place(Request(), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Code.ShouldBeOfType<PlacementUserUnknownCode>();
        rig.Inserts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task ATenantWithNoDefaultOrganizationIsRefusedAndNothingIsWritten()
    {
        var rig = new Rig();
        rig.DefaultOrganizations([]);

        var placed = await rig.Placement().Place(Request(), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Code.ShouldBeOfType<DefaultOrganizationMissingCode>();
        rig.Inserts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AnOrganizationThatNamesNoDefaultWorkspaceIsRefusedAndNothingIsWritten()
    {
        var rig = new Rig();
        rig.DefaultOrganizations([new DefaultOrganizationRow { Id = Organization, Name = "Default Organization" }]);

        var placed = await rig.Placement().Place(Request(), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Code.ShouldBeOfType<DefaultWorkspaceMissingCode>();
        rig.Inserts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task WithTheSettingOffNothingIsPlacedAndNothingIsRefusedForTheLackOfAWorkspace()
    {
        var rig = new Rig();
        rig.Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Success(false));
        rig.Authentication.Current = null;
        rig.DefaultOrganizations([]);

        var placed = await rig.Placement().Place(Request(), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue(placed.CurrentMessage);
        rig.Inserts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task WithTheSettingOffAKindThatIsNotAWorkspaceResourceKindIsStillRefused()
    {
        var rig = new Rig();
        rig.Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Success(false));

        var placed = await rig.Placement().Place(Request("Sandwich"), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Code.ShouldBeOfType<PlacementKindUnknownCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task ASettingThatCannotBeReadRefusesThePlacementAndNothingIsWritten()
    {
        var rig = new Rig();
        rig.Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Failure(new GenericMessage("no setting")));

        var placed = await rig.Placement().Place(Request(), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        rig.Inserts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task ADefaultWorkspaceWithNoCurrentConfigurationIsRefusedAndNothingIsWritten()
    {
        var rig = new Rig(configurationRow: false);

        var placed = await rig.Placement().Place(Request(), await rig.Unit(), TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Code.ShouldBeOfType<DefaultWorkspaceMissingCode>();
        rig.Inserts.ShouldBeEmpty();
    }
}
