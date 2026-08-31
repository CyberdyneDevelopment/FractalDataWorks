using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// Proves the keystone base read mechanism composes the Escalation aggregate
/// (Policy → Levels → Recipients) physically — i.e. <see cref="EscalationConfigurationProvider"/>.Get(id)
/// returns a policy with its Levels populated and each Level with its Recipients populated, driven by the
/// mapper's CascadeChildren descriptors. RowId is INVISIBLE to the app (a DB-managed IDENTITY, never a
/// POCO property): each child set is read by JOINing the child to its owner ON owner.{PhysicalKey}=child.{Owner}RowId
/// and filtering by the owner's DURABLE Id — so the RowId↔RowId match is resolved entirely in the DB and
/// every owner container must carry both a Physical ("RowId") and a Logical ("Id") key in the schema tree.
/// This is the read half of Decision-A's physical-keying convergence.
/// </summary>
public sealed class EscalationAggregateCompositionTests
{
    private static readonly Guid PolicyId = Guid.NewGuid();
    private static readonly Guid Level1Id = Guid.NewGuid();
    private static readonly Guid Level2Id = Guid.NewGuid();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetComposesPolicyLevelsAndRecipientsViaBaseMechanism()
    {
        var gateway = new AggregateGateway();
        var provider = new EscalationConfigurationProvider(
            NullLogger<EscalationConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "PlatformConfiguration",
            "workflow");

        var result = await provider.Get(PolicyId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        // Levels composed off the policy's CascadeChildren (child JOIN owner on EscalationPolicy.RowId =
        // EscalationLevel.EscalationPolicyRowId, filtered by EscalationPolicy.Id = policy.Id).
        result.Value!.Levels.Count.ShouldBe(2);
        // Recipients composed one level deeper (child JOIN owner on EscalationLevel.RowId =
        // EscalationLevelRecipient.EscalationLevelRowId, filtered by EscalationLevel.Id = level.Id) — the
        // nested cascade. RowId is never materialized; levels are found by their durable Id.
        var level1 = result.Value.Levels.Single(l => l.Id == Level1Id);
        var level2 = result.Value.Levels.Single(l => l.Id == Level2Id);
        level1.Level.ShouldBe(1);
        level2.Level.ShouldBe(2);
        level1.Recipients.Count.ShouldBe(2);
        level2.Recipients.Count.ShouldBe(1);
    }

    // ── Fake gateway: a tiny in-memory ConfigurationDb.workflow tree + row tables ──────────────────
    private sealed class AggregateGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly IReadOnlyList<IDataStore> _stores;
        private readonly List<EscalationPolicyConfiguration> _policies;
        private readonly List<EscalationLevelConfiguration> _levels;
        private readonly Dictionary<Guid, List<EscalationLevelRecipientConfiguration>> _recipientsByLevelId;

        public AggregateGateway()
        {
            _policies =
            [
                new EscalationPolicyConfiguration { Id = PolicyId, Name = "P1", IsEnabled = true }
            ];
            // Single policy in this fixture, so the level read returns all levels for the policy.
            _levels =
            [
                new EscalationLevelConfiguration { Id = Level1Id, Level = 1 },
                new EscalationLevelConfiguration { Id = Level2Id, Level = 2 }
            ];
            // Recipients keyed by the owning level's DURABLE Id (RowId is invisible to the app).
            _recipientsByLevelId = new Dictionary<Guid, List<EscalationLevelRecipientConfiguration>>
            {
                [Level1Id] =
                [
                    new EscalationLevelRecipientConfiguration { Id = Guid.NewGuid(), Recipient = "a@x" },
                    new EscalationLevelRecipientConfiguration { Id = Guid.NewGuid(), Recipient = "b@x" }
                ],
                [Level2Id] =
                [
                    new EscalationLevelRecipientConfiguration { Id = Guid.NewGuid(), Recipient = "c@x" }
                ]
            };
            _stores = [BuildTree()];
        }

        public IReadOnlyList<IDataStore> DataStores => _stores;

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        // Header read: WHERE Id = @id → the single policy.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<EscalationPolicyConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_policies.AsEnumerable()));
            return Task.FromResult(GenericResult<T>.Success((T)(object)Array.Empty<object>().AsEnumerable()));
        }

        // By-type child read: the new query is a JOIN to the owner filtered by the owner's durable Id.
        // Levels: single policy → return all levels. Recipients: return the rows for the level Id carried
        // in the JOIN filter (the owner.Id condition), resolved without parsing a single RowId condition.
        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            if (rowType == typeof(EscalationLevelConfiguration))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_levels.Cast<object>()));
            if (rowType == typeof(EscalationLevelRecipientConfiguration))
            {
                var ownerId = ExtractOwnerIdFromJoinFilter(command);
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(
                    (_recipientsByLevelId.TryGetValue(ownerId, out var recips) ? recips : []).Cast<object>()));
            }
            return Task.FromResult(GenericResult<IEnumerable<object>>.Success(Enumerable.Empty<object>()));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not used in this test")));

        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not used in this test")));

        private static Guid ExtractOwnerIdFromJoinFilter(IDataCommand command)
        {
            if (command is not QueryCommand<object> q || q.Filter?.Root is not IFilterNode root)
                return Guid.Empty;
            foreach (var condition in Flatten(root))
            {
                if (condition.Value is Guid g &&
                    condition.PropertyName.EndsWith(".Id", StringComparison.Ordinal))
                    return g;
            }
            return Guid.Empty;
        }

        private static IEnumerable<FilterCondition> Flatten(IFilterNode node)
        {
            switch (node)
            {
                case FilterCondition fc:
                    yield return fc;
                    break;
                case FilterGroup fg:
                    foreach (var child in fg.Nodes)
                        foreach (var leaf in Flatten(child))
                            yield return leaf;
                    break;
            }
        }

        private static IDataStore BuildTree()
        {
            // Leaf container (no children) needs no keys. Owner containers at every level need BOTH a
            // Physical key (field "RowId" — the JOIN target the child FK points at) and a Logical key
            // (field "Id" — the durable-Id filter), or ResolveOwnerKeyColumns returns null and the owner's
            // children silently fail to load.
            var recipientContainer = Container("EscalationLevelRecipient", referencing: [], keys: null);
            var levelContainer = Container("EscalationLevel",
                referencing: [Binding("EscalationLevelRowId", recipientContainer)],
                keys: [Key("Physical", "PK_EscalationLevel", "RowId"), Key("Logical", "AK_EscalationLevel", "Id")]);
            var policyContainer = Container("EscalationPolicy",
                referencing: [Binding("EscalationPolicyRowId", levelContainer)],
                keys: [Key("Physical", "PK_EscalationPolicy", "RowId"), Key("Logical", "AK_EscalationPolicy", "Id")]);

            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("workflow");
            var containers = new List<IDataContainer> { policyContainer, levelContainer, recipientContainer };
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string n) =>
            {
                var c = containers.FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.Ordinal));
                return c is null
                    ? GenericResult<IDataContainer>.Failure(new GenericMessage("not found"))
                    : GenericResult<IDataContainer>.Success(c);
            });
            foreach (var c in containers)
                Mock.Get(c).Setup(x => x.Parent).Returns(path.Object);

            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("PlatformConfiguration");
            store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "workflow", StringComparison.Ordinal)
                    ? GenericResult<IDataNodePath>.Success(path.Object)
                    : GenericResult<IDataNodePath>.Failure(new GenericMessage("not found")));
            return store.Object;
        }

        private static IDataContainer Container(
            string name, IReadOnlyList<ReferencingKeyBinding> referencing, IReadOnlyList<IContainerKey>? keys)
        {
            var c = new Mock<IDataContainer>();
            c.Setup(x => x.Name).Returns(name);
            c.Setup(x => x.Keys).Returns(keys ?? new List<IContainerKey>());
            c.Setup(x => x.Nodes).Returns(new List<IDataNode>());
            c.Setup(x => x.ReferencingKeys).Returns(
                GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
            return c.Object;
        }

        private static ReferencingKeyBinding Binding(string fkColumn, IDataContainer owner)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(fkColumn);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyName).Returns($"FK_{fkColumn}");
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            return new ReferencingKeyBinding(key.Object, owner);
        }

        private static IContainerKey Key(string keyType, string keyName, string localField)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(localField);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            KeyTypeBase kt = keyType switch
            {
                "Physical" => new PhysicalKeyType(),
                "Logical" => new LogicalKeyType(),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "unsupported key type in test")
            };
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(kt);
            key.Setup(k => k.KeyName).Returns(keyName);
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            key.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);
            return key.Object;
        }
    }

    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}
