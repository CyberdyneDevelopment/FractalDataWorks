using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Shouldly;
using Moq;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Smoke test for <see cref="DefaultConfigurationProvider{TConfig,TCommand}.Get(Guid, CancellationToken)"/>
/// verifying that when the container has a Foreign key (no Primary key), the emitted command
/// uses the FK column rather than [Id].
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class DefaultConfigurationProviderGetByIdTests
{
    // ========================================================================
    // Smoke test: FK column emitted for child container
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetByIdUsesPhysicalForeignKeyFilterWhenContainerHasParentFK()
    {
        // Arrange: container with Foreign key only (no Primary, no Natural). Models the
        // sec.AzureKeyVaultSecretManager case — the physical FK column (SecretManagerRowId)
        // is returned directly from the metadata; the caller passes the parent's RowId.
        var fakeGateway = BuildChildConfigFixture(
            storeName: "ConfigurationDb",
            pathName: "sec",
            containerName: "AzureKeyVaultSecretManager",
            fkColumn: "SecretManagerId");


        var provider = new DefaultConfigurationProvider<TestChildConfig, TestChildCommand>(
            NullLogger<DefaultConfigurationProvider<TestChildConfig, TestChildCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => fakeGateway),
            "ConfigurationDb",
            "sec");

        var domainConfigurationId = Guid.NewGuid();
        await provider.Get(domainConfigurationId, TestContext.Current.CancellationToken);

        // Assert: the FK join filters by the PARENT's durable Logical key ("SecretManager.Id"); the
        // caller passes the parent's Id as the value. The join column itself (SecretManagerRowId →
        // SecretManager.RowId) is on the JOIN clause, not the WHERE filter.
        fakeGateway.LastCommand.ShouldNotBeNull();
        fakeGateway.LastCommand.ShouldBeAssignableTo<QueryCommand<TestChildConfig>>();
        var qc = (QueryCommand<TestChildConfig>)fakeGateway.LastCommand!;
        var filterNode = ExtractKeyPredicate(qc.Filter);
        filterNode.ShouldNotBeNull();
        filterNode!.PropertyName.ShouldBe("SecretManager.Id");
        filterNode.Value.ShouldBe(domainConfigurationId);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    // Why: Walks past the IsCurrent/IsDeleted conditions to find the key-specific predicate.
    // When the filter is a FilterGroup (AND group), the last node is the key condition.
    // When the filter is a leaf FilterCondition, it IS the key condition.
    private static FilterCondition? ExtractKeyPredicate(IFilterExpression? filter)
    {
        if (filter?.Root is null) return null;

        if (filter.Root is FilterCondition single)
            return single;

        // Root is a FilterGroup — key predicate is the last node (IsCurrent and IsDeleted
        // come first from the base query builder).
        if (filter.Root is Fdw.Data.FilterGroup group && group.Nodes.Count > 0)
        {
            // The merged filter has IsCurrent+IsDeleted in the baseFilter group, and the
            // key condition is the second child of the outer AND group.
            // Walk all nodes to find the FilterCondition that is NOT IsCurrent or IsDeleted.
            return FindKeyNode(group);
        }

        return null;
    }

    private static FilterCondition? FindKeyNode(Fdw.Data.FilterGroup group)
    {
        foreach (var node in group.Nodes)
        {
            if (node is FilterCondition fc)
            {
                var prop = fc.PropertyName;
                if (!string.Equals(prop, "IsCurrent", StringComparison.Ordinal) &&
                    !string.Equals(prop, "IsDeleted", StringComparison.Ordinal))
                    return fc;
            }
            else if (node is Fdw.Data.FilterGroup nested)
            {
                var found = FindKeyNode(nested);
                if (found is not null) return found;
            }
        }
        return null;
    }

    /// <summary>
    /// Builds a minimal in-memory IDataStore tree and a capturing fake gateway.
    /// The container exposes only a Foreign key on <paramref name="fkColumn"/>.
    /// </summary>
    private static CapturingGateway
        BuildChildConfigFixture(
            string storeName,
            string pathName,
            string containerName,
            string fkColumn)
    {
        // Why: real metadata exposes FK keys via the PHYSICAL column name (e.g. SecretManagerRowId).
        // FindForeignKeyColumn returns that physical column directly — no string-stripping needed.
        // The fixture mirrors that shape: FK on "{fkColumn-without-Id}RowId". The logical field
        // (SecretManagerId) is kept in the Fields list but is no longer used by the resolver.
        var fkBase = fkColumn.EndsWith("Id", StringComparison.Ordinal)
            ? fkColumn[..^"Id".Length]
            : fkColumn;
        var physicalFkColumn = fkBase + "RowId";

        var mockPhysicalField = new Mock<IDataField>();
        mockPhysicalField.Setup(f => f.Name).Returns(physicalFkColumn);

        var mockLogicalField = new Mock<IDataField>();
        mockLogicalField.Setup(f => f.Name).Returns(fkColumn);

        var mockKeyField = new Mock<IContainerKeyField>();
        mockKeyField.Setup(k => k.LocalField).Returns(mockPhysicalField.Object);
        mockKeyField.Setup(k => k.Ordinal).Returns(0);
        mockKeyField.Setup(k => k.ReferencedField).Returns((IDataField?)null);

        // Why: ResolveParentJoin reads the FK's ReferencedContainer (the parent) and joins on the
        // parent's Physical key (RowId — the FK target) while filtering by the parent's Logical key
        // (the durable Id). The fixture supplies a parent container carrying both keys.
        var parentName = fkBase;
        var mockParentPhysicalField = new Mock<IDataField>();
        mockParentPhysicalField.Setup(f => f.Name).Returns("RowId");
        var mockParentPhysicalKeyField = new Mock<IContainerKeyField>();
        mockParentPhysicalKeyField.Setup(k => k.LocalField).Returns(mockParentPhysicalField.Object);
        var mockParentPhysicalKey = new Mock<IContainerKey>();
        mockParentPhysicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Physical);
        mockParentPhysicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { mockParentPhysicalKeyField.Object });
        mockParentPhysicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var mockParentLogicalField = new Mock<IDataField>();
        mockParentLogicalField.Setup(f => f.Name).Returns("Id");
        var mockParentLogicalKeyField = new Mock<IContainerKeyField>();
        mockParentLogicalKeyField.Setup(k => k.LocalField).Returns(mockParentLogicalField.Object);
        var mockParentLogicalKey = new Mock<IContainerKey>();
        mockParentLogicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Logical);
        mockParentLogicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { mockParentLogicalKeyField.Object });
        mockParentLogicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var mockParentContainer = new Mock<IDataContainer>();
        mockParentContainer.Setup(c => c.Name).Returns(parentName);
        mockParentContainer.Setup(c => c.Keys).Returns(new List<IContainerKey>
        {
            mockParentPhysicalKey.Object,
            mockParentLogicalKey.Object
        });

        // Build IContainerKey with KeyType = ForeignKeyType, referencing the parent container.
        var mockKey = new Mock<IContainerKey>();
        mockKey.Setup(k => k.KeyType).Returns(KeyTypes.Foreign);
        mockKey.Setup(k => k.KeyName).Returns($"FK_{containerName}");
        mockKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { mockKeyField.Object });
        mockKey.Setup(k => k.IsPhysical).Returns(true);
        mockKey.Setup(k => k.ReferencedContainer).Returns(mockParentContainer.Object);

        // Build IDataContainer — fields list includes the logical fk column so FindLogicalFkColumn
        // can locate it after stripping "RowId" from the physical name.
        var mockContainer = new Mock<IDataContainer>();
        mockContainer.Setup(c => c.Name).Returns(containerName);
        mockContainer.Setup(c => c.Keys).Returns(new List<IContainerKey> { mockKey.Object });
        mockContainer.Setup(c => c.Nodes).Returns(new List<IDataNode> { mockLogicalField.Object });
        mockContainer.Setup(c => c.Description).Returns((string?)null);

        // Build IDataNodePath with Container() lookup — returns IGenericResult<IDataContainer>.
        // Why: the typed-body parent lives in the SAME path as the child (e.g. sec.SecretManager and
        // sec.AzureKeyVaultSecretManager), so the path resolves BOTH the child and its parent. The FK
        // selector uses this to distinguish the parent FK from a cross-path data reference.
        var mockPath = new Mock<IDataNodePath>();
        mockPath.Setup(p => p.Name).Returns(pathName);
        mockPath.Setup(p => p.Containers).Returns(new List<IDataContainer> { mockContainer.Object, mockParentContainer.Object });
        mockPath.Setup(p => p.Container(It.Is<string>(n =>
            string.Equals(n, containerName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Success(mockContainer.Object));
        mockPath.Setup(p => p.Container(It.Is<string>(n =>
            string.Equals(n, parentName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Success(mockParentContainer.Object));
        mockPath.Setup(p => p.Container(It.Is<string>(n =>
            !string.Equals(n, containerName, StringComparison.Ordinal) &&
            !string.Equals(n, parentName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));

        // Build IDataStore with Path() lookup — returns IGenericResult<IDataNodePath>
        var mockStore = new Mock<IDataStore>();
        mockStore.Setup(s => s.Name).Returns(storeName);
        mockStore.Setup(s => s.Paths).Returns(new List<IDataNodePath> { mockPath.Object });
        mockStore.Setup(s => s.Path(It.Is<string>(n =>
            string.Equals(n, pathName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Success(mockPath.Object));
        mockStore.Setup(s => s.Path(It.Is<string>(n =>
            !string.Equals(n, pathName, StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Failure(new GenericMessage("path not found")));
        mockStore.Setup(s => s.ConnectionId).Returns(Guid.NewGuid());

        IReadOnlyList<IDataStore> stores = new List<IDataStore> { mockStore.Object };

        // Why: ResolveParentJoin now reads the bounded ConfigurationDb schema tree from
        // IConfigurationGateway.DataStores (the eager full-tree singleton is gone), so the fixture
        // exposes the mock tree through the capturing gateway rather than a separate Lazy parameter.
        var gateway = new CapturingGateway { DataStores = stores };
        return gateway;
    }

    // ========================================================================
    // Test infrastructure types
    // ========================================================================

    /// <summary>
    /// Minimal test configuration type for child config scenario.
    /// </summary>
    public sealed class TestChildConfig : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestChildSection";
        public string ServiceType => "TestChild";
        public string? ServiceOptionType => "Default";
        public Guid SecretManagerId { get; set; }
    }

    /// <summary>
    /// TypeOption for TestChildConfig — registered via <see cref="ServicesTypeCollectionFixture"/>.
    /// </summary>
    // Why: ConfigurationCommands TypeCollection is populated by source generators for referenced
    // assemblies only. Test-assembly types must be registered manually in the fixture.
    [TypeOption(typeof(ConfigurationCommands), "TestChildConfig")]
    public sealed class TestChildCommand : ConfigurationCommandBase<TestChildConfig>
    {
        public TestChildCommand() : base("AzureKeyVaultSecretManager") { }
    }

    /// <summary>
    /// Fake IConfigurationGateway that records the last command it receives.
    /// </summary>
    private sealed class CapturingGateway : IConfigurationGateway
    {
        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        /// <summary>Gets the last command passed to Execute&lt;T&gt;.</summary>
        public IDataCommand? LastCommand { get; private set; }

        /// <inheritdoc/>
        // Why: test double exposes the fixture's mock schema tree so ResolveParentJoin can resolve the
        // child→parent FK join from container metadata (the eager full-tree singleton is gone).
        public IReadOnlyList<IDataStore> DataStores { get; init; } = [];

        /// <inheritdoc/>
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        /// <inheritdoc/>
        // Why: test double — useCache not exercised in command-routing unit tests; delegates to existing implementation.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        /// <inheritdoc/>
        // Why: test double — records the command and returns success; target addressing is not exercised.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        /// <inheritdoc/>
        // Why: test double — non-generic save path; records the command like the typed overload.
        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        /// <inheritdoc/>
        // Why: test double — by-type child read records the command and returns no rows; these tests
        // never reach child composition (the header read returns a null row), so an empty result suffices.
        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));
        }

        /// <inheritdoc/>
        // Why: test double — DataSet routing not exercised in command-routing unit tests.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in CapturingGateway test double")));

        /// <inheritdoc/>
        // Why: test double — transactions are not needed for command-routing unit tests.
        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));
    }
}
