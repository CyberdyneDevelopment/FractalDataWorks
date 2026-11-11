using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// FDW-5222: deleting a role removes every assignment of it to a person and every permission it grants, in the same unit of work as the
/// role, before the role itself goes. A role deleted while people held it used to leave their assignments behind, and each holder's own
/// profile then failed to read.
/// </summary>
public sealed class RoleDeleteTests
{
    private static readonly Guid RoleId = Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000a1");

    private readonly Mock<IConfigurationGateway> _gateway = new();
    private readonly GatewayWriteTransaction _transaction;
    private readonly Mock<IConfigurationSupersedeParticipant<IRoleImplementationConfiguration>> _roleBody = new();
    private readonly Mock<IUserRoleConfigurationProvider> _userRoles = new();
    private readonly Mock<IRolePermissionConfigurationProvider> _rolePermissions = new();
    private readonly List<string> _steps = [];
    private readonly List<ConfigurationWriteScope> _units = [];
    private readonly RoleConfigurationProvider _roles;

    public RoleDeleteTests()
    {
        _transaction = GatewayWriteTransaction.StubOn(_gateway);

        // The unit resolves the domain container from the loaded store before it retires anything.
        var container = new Mock<IDataContainer>();
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Container(It.IsAny<string>())).Returns(GenericResult<IDataContainer>.Success(container.Object));
        var store = new Mock<IDataStore>();
        store.SetupGet(s => s.Name).Returns("TestStore");
        store.Setup(s => s.Path(It.IsAny<string>())).Returns(GenericResult<IDataNodePath>.Success(path.Object));
        _gateway.SetupGet(g => g.DataStores).Returns([store.Object]);

        // The role's domain row, found by its Id, and its key values.
        _gateway
            .Setup(g => g.Execute<IEnumerable<RoleDomainConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<RoleDomainConfiguration>>.Success(
                [new RoleDomainConfiguration { Id = RoleId, Name = "Analyst", Domain = "Role", Implementation = "Role" }]));
        _gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([new Dictionary<string, object?>(StringComparer.Ordinal) { ["RowId"] = 7 }]));

        // The retire of the role's domain row, the last write of the delete.
        _gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback(() => _steps.Add("role row"))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(
                new ConfigurationWriteResult([new Dictionary<string, object?>(StringComparer.Ordinal) { ["RowId"] = 7 }], null)));

        _roleBody
            .Setup(p => p.Delete(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback((IDataContainer _, IReadOnlyDictionary<string, object?> _, ConfigurationWriteScope unit, CancellationToken _) =>
            {
                _steps.Add("role body");
                _units.Add(unit);
            })
            .ReturnsAsync(GenericResult.Success());

        Assignments(GenericResult<int>.Success(2));
        Grants(GenericResult<int>.Success(3));

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.SetupGet(p => p.KeyChunkSize).Returns(1000);
        gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(_gateway.Object));

        _roles = new RoleConfigurationProvider(
            NullLogger<RoleConfigurationProvider>.Instance, gateways.Object, "TestStore", "authz", "Role", _userRoles.Object, _rolePermissions.Object);
        _roles.Register("Role", _roleBody.Object).IsSuccess.ShouldBeTrue();
    }

    private void Assignments(IGenericResult<int> answer)
        => _userRoles
            .Setup(p => p.RemoveAssignmentsOf(It.IsAny<Guid>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, ConfigurationWriteScope unit, CancellationToken _) =>
            {
                _steps.Add("assignments");
                _units.Add(unit);
            })
            .ReturnsAsync(answer);

    private void Grants(IGenericResult<int> answer)
        => _rolePermissions
            .Setup(p => p.RemoveGrantsOf(It.IsAny<Guid>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, ConfigurationWriteScope unit, CancellationToken _) =>
            {
                _steps.Add("grants");
                _units.Add(unit);
            })
            .ReturnsAsync(answer);

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeletingARoleRemovesItsAssignmentsAndGrantsBeforeTheRoleInOneUnit()
    {
        var deleted = await _roles.Delete(RoleId, TestContext.Current.CancellationToken);

        deleted.IsSuccess.ShouldBeTrue(deleted.CurrentMessage);
        _steps.ToArray().ShouldBe(new[] { "assignments", "grants", "role body", "role row" });
        _units.Count.ShouldBe(3);
        _units.ShouldAllBe(unit => ReferenceEquals(unit, _units[0]));
        _userRoles.Verify(p => p.RemoveAssignmentsOf(RoleId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _rolePermissions.Verify(p => p.RemoveGrantsOf(RoleId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Committed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WhenTheAssignmentsCannotBeRemovedTheRoleStaysAndNothingCommits()
    {
        Assignments(GenericResult<int>.Failure(new GenericMessage("the assignments could not be read")));

        var deleted = await _roles.Delete(RoleId, TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        _steps.ToArray().ShouldBe(new[] { "assignments" });
        _transaction.Committed.ShouldBeFalse();
        _transaction.RolledBack.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WhenTheGrantsCannotBeRemovedTheRoleStaysAndTheAssignmentsRollBack()
    {
        Grants(GenericResult<int>.Failure(new GenericMessage("a grant could not be retired")));

        var deleted = await _roles.Delete(RoleId, TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        _steps.ToArray().ShouldBe(new[] { "assignments", "grants" });
        _transaction.Committed.ShouldBeFalse();
        _transaction.RolledBack.ShouldBeTrue();
    }

    [Fact]
    public async Task ARoleThatIsNotThereRemovesNothing()
    {
        _gateway
            .Setup(g => g.Execute<IEnumerable<RoleDomainConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<RoleDomainConfiguration>>.Success([]));

        var deleted = await _roles.Delete(RoleId, TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        _steps.ShouldBeEmpty();
    }
}
