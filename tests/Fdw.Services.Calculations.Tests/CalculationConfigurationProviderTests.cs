using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Proves a read through <see cref="CalculationConfigurationProvider"/> composes the FULL calculation:
/// the domain row names the implementation, the registered implementation provider reads its own row,
/// and the child cascade fills Inputs and Steps→{Fields, Operands} (recursive, joined on the owner's
/// RowId). Only <see cref="IConfigurationGateway"/> is faked.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public class CalculationConfigurationProviderTests
{
    private static readonly Guid EntityId = Guid.NewGuid();
    private static readonly Guid StepId = Guid.NewGuid();

    [Fact]
    public async Task GetComposesTheImplementationWithItsInputsStepsFieldsAndOperands()
    {
        var gateway = new AggregateGateway();
        var domain = new CalculationConfigurationProvider(
            NullLogger<CalculationConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "PlatformConfiguration");

        // Registered exactly as DefaultCalculationServiceType registers it.
        domain.Register("Formula", new FormulaCalculationConfigurationProvider(
            NullLogger<FormulaCalculationConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "PlatformConfiguration"));

        var result = await domain.Get(EntityId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var formula = result.Value.ShouldBeOfType<FormulaCalculationConfiguration>();
        formula.FormulaBody.ShouldBe("[A]+[B]");
        formula.Inputs.Count.ShouldBe(2);
        formula.Steps.Count.ShouldBe(1);
        formula.Steps[0].Fields.Count.ShouldBe(2);
        formula.Steps[0].Operands.Count.ShouldBe(1);

        // The domain row is what names the member; the implementation carries the rest.
        formula.Name.ShouldBe("Calc1");
        formula.Implementation.ShouldBe("Formula");
    }

    private sealed class AggregateGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly IReadOnlyList<IDataStore> _stores;
        private readonly List<DomainConfiguration> _rows;
        private readonly List<CalculationEntityInputRecord> _inputs;
        private readonly List<CalculationStepConfiguration> _steps;
        private readonly List<CalculationStepFieldConfiguration> _fields;
        private readonly List<CalculationStepOperandConfiguration> _operands;
        private readonly List<FormulaCalculationConfiguration> _formula;

        public AggregateGateway()
        {
            _rows =
            [
                new DomainConfiguration
                {
                    Id = EntityId, Name = "Calc1", Domain = "CalculationEntity", Implementation = "Formula",
                }
            ];
            _inputs =
            [
                new CalculationEntityInputRecord { Id = Guid.NewGuid(), InputAlias = "A", InputKind = "DataSet", Ordinal = 0 },
                new CalculationEntityInputRecord { Id = Guid.NewGuid(), InputAlias = "B", InputKind = "DataSet", Ordinal = 1 }
            ];
            _steps = [new CalculationStepConfiguration { Id = StepId, Name = "Step1", OperationType = "Add", OutputAlias = "S1" }];
            _fields =
            [
                new CalculationStepFieldConfiguration { Id = Guid.NewGuid(), StepFieldRole = "GroupBy", Ordinal = 0 },
                new CalculationStepFieldConfiguration { Id = Guid.NewGuid(), StepFieldRole = "OrderBy", Ordinal = 1 }
            ];
            _operands = [new CalculationStepOperandConfiguration { Id = Guid.NewGuid(), Name = "op1", OperandType = "Input", InputAlias = "A" }];
            _formula =
            [
                new FormulaCalculationConfiguration
                {
                    Id = EntityId, CalculationEntityId = EntityId, FormulaBody = "[A]+[B]", FormulaLanguage = "CSharp",
                }
            ];
            _stores = [BuildTree()];
        }

        public IReadOnlyList<IDataStore> DataStores => _stores;

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_rows.AsEnumerable()));
            if (typeof(T) == typeof(IEnumerable<FormulaCalculationConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_formula.AsEnumerable()));
            return Task.FromResult(GenericResult<T>.Success((T)(object)Array.Empty<object>().AsEnumerable()));
        }

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            if (rowType == typeof(CalculationEntityInputRecord))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_inputs.Cast<object>()));
            if (rowType == typeof(CalculationStepConfiguration))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_steps.Cast<object>()));
            if (rowType == typeof(CalculationStepFieldConfiguration))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_fields.Cast<object>()));
            if (rowType == typeof(CalculationStepOperandConfiguration))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_operands.Cast<object>()));
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

        private static IDataStore BuildTree()
        {
            var inputContainer = Container("CalculationEntityInputRecord", null);
            var fieldContainer = Container("CalculationStepField", null);
            var operandContainer = Container("CalculationStepOperand", null);

            // CalculationStep owns fields and operands, so the child read resolves its Physical (RowId)
            // and Logical (Id) key columns from metadata to build the join — it needs both.
            var stepContainer = Container("CalculationStep",
                [Key("Physical", "PK_CalculationStep", "RowId", null), Key("Logical", "AK_CalculationStep", "Id", null)]);

            // The domain container the implementation row joins back to.
            var entityContainer = Container("CalculationEntity",
                [Key("Physical", "PK_CalculationEntity", "RowId", null), Key("Logical", "AK_CalculationEntity", "Id", null)]);

            // The implementation container: it carries the outbound Foreign key to its domain, and its
            // own keys, because it is the owner the children hang from.
            var formulaContainer = Container("FormulaCalculation",
            [
                Key("Foreign", "FK_FormulaCalculation_CalculationEntity", "CalculationEntityRowId", entityContainer),
                Key("Physical", "PK_FormulaCalculation", "RowId", null),
                Key("Logical", "AK_FormulaCalculation", "Id", null)
            ]);

            var containers = new List<IDataContainer>
            { entityContainer, formulaContainer, inputContainer, stepContainer, fieldContainer, operandContainer };

            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("calc");
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string n) =>
            {
                var c = containers.FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.Ordinal));
                return c is null ? GenericResult<IDataContainer>.Failure(new GenericMessage("nf")) : GenericResult<IDataContainer>.Success(c);
            });
            foreach (var c in containers)
                Mock.Get(c).Setup(x => x.Parent).Returns(path.Object);

            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("PlatformConfiguration");
            store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "calc", StringComparison.Ordinal)
                    ? GenericResult<IDataNodePath>.Success(path.Object)
                    : GenericResult<IDataNodePath>.Failure(new GenericMessage("nf")));
            return store.Object;
        }

        private static IDataContainer Container(string name, IReadOnlyList<IContainerKey>? keys)
        {
            var c = new Mock<IDataContainer>();
            c.Setup(x => x.Name).Returns(name);
            c.Setup(x => x.Keys).Returns(keys ?? new List<IContainerKey>());
            c.Setup(x => x.Nodes).Returns(new List<IDataNode>());
            c.Setup(x => x.ReferencingKeys).Returns(
                GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(new List<ReferencingKeyBinding>()));
            return c.Object;
        }

        private static IContainerKey Key(string keyType, string keyName, string localField, IDataContainer? referenced)
        {
            var field = new Mock<global::Fdw.Data.Abstractions.IDataField>();
            field.Setup(f => f.Name).Returns(localField);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            global::Fdw.Data.Abstractions.KeyTypeBase kt = keyType switch
            {
                "Foreign" => new global::Fdw.Data.Abstractions.ForeignKeyType(),
                "Physical" => new global::Fdw.Data.Abstractions.PhysicalKeyType(),
                "Logical" => new global::Fdw.Data.Abstractions.LogicalKeyType(),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "unsupported key type in test")
            };
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(kt);
            key.Setup(k => k.KeyName).Returns(keyName);
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            key.Setup(k => k.ReferencedContainer).Returns(referenced);
            return key.Object;
        }

        // The fixture addresses every call; a gateway's generic command surface carries no address,
        // so nothing here routes one and reaching these would be the test lying about the seam.
        string IPlatformService.Id => "AggregateGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");
    }

    private static AnyConnectionGateways GatewayProviderFor(IConfigurationGateway gateway)
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
