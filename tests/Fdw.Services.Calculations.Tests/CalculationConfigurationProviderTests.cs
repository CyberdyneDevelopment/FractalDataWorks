using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Calculations.Commands;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Proves the keystone base read composes the FULL calculation aggregate via
/// <see cref="CalculationConfigurationProvider"/>.Get(id): header → Inputs, Steps→{Fields,Operands}
/// (recursive, physically keyed by RowId), plus the polymorphic Formula typed body composed via the
/// registered typed provider (dispatch on ServiceOptionType). Only IConfigurationGateway is faked.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public class CalculationConfigurationProviderTests
{
    private static readonly Guid EntityId = Guid.NewGuid();
    private static readonly Guid StepId = Guid.NewGuid();

    [Fact]
    public async Task GetComposesInputsStepsFieldsOperandsAndTypedBody()
    {
        var gateway = new AggregateGateway();
        var provider = new CalculationConfigurationProvider(
            NullLogger<CalculationConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway),
            "ConfigurationDb",
            "calc");

        // Register the Formula typed provider exactly as DefaultCalculationServiceType.RegisterFactory does.
        var formulaProvider = new DefaultConfigurationProvider<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>(
            NullLogger<DefaultConfigurationProvider<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway),
            "ConfigurationDb",
            "calc");
        provider.Register("Formula", formulaProvider);

        var result = await provider.Get(EntityId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Inputs.Count.ShouldBe(2);
        result.Value.Steps.Count.ShouldBe(1);
        result.Value.Steps[0].Fields.Count.ShouldBe(2);
        result.Value.Steps[0].Operands.Count.ShouldBe(1);
        result.Value.Configuration.ShouldBeOfType<FormulaCalculationConfiguration>();
        ((FormulaCalculationConfiguration)result.Value.Configuration!).FormulaBody.ShouldBe("[A]+[B]");
    }

    // Why: hand-written so the real RowId-keyed read runs — header by [Id], typed body by child->parent
    // JOIN, child collections filtered by the parent RowId and matched via each child's
    // ConfigurationCommand.ContainerName, recursing one level into the step.
    private sealed class AggregateGateway : IConfigurationGateway
    {
        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly IReadOnlyList<IDataStore> _stores;
        private readonly List<CalculationEntityConfiguration> _entities;
        private readonly List<CalculationEntityInputRecord> _inputs;
        private readonly List<CalculationStepConfiguration> _steps;
        private readonly List<CalculationStepFieldConfiguration> _fields;
        private readonly List<CalculationStepOperandConfiguration> _operands;
        private readonly List<FormulaCalculationConfiguration> _formula;

        public AggregateGateway()
        {
            _entities = [new CalculationEntityConfiguration { Id = EntityId, Name = "Calc1", CalculationEntityType = "Formula" }];
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
            _formula = [new FormulaCalculationConfiguration { Id = Guid.NewGuid(), CalculationEntityId = EntityId, FormulaBody = "[A]+[B]", FormulaLanguage = "CSharp" }];
            _stores = [BuildTree()];
        }

        public IReadOnlyList<IDataStore> DataStores => _stores;

        // Why: test double — useCache not exercised in calculation provider tests; delegates to existing implementation.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<CalculationEntityConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_entities.AsEnumerable()));
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

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not used in this test")));

        private IDataStore BuildTree()
        {
            var inputContainer = Container("CalculationEntityInput", [], null);
            var fieldContainer = Container("CalculationStepField", [], null);
            var operandContainer = Container("CalculationStepOperand", [], null);
            // CalculationStep is itself an owner of fields/operands — the new child read resolves its
            // Physical (RowId) + Logical (Id) key columns from metadata to build the JOIN, so it needs both.
            var stepContainer = Container("CalculationStep",
            [
                Binding("CalculationStepRowId", fieldContainer),
                Binding("CalculationStepRowId", operandContainer)
            ],
            [Key("Physical", "PK_CalculationStep", "RowId", null), Key("Logical", "AK_CalculationStep", "Id", null)]);

            // CalculationEntity is the FK target for the typed body — give it Physical + Logical keys.
            var entityContainer = Container("CalculationEntity",
            [
                Binding("CalculationEntityRowId", inputContainer),
                Binding("CalculationEntityRowId", stepContainer)
            ],
            [Key("Physical", "PK_CalculationEntity", "RowId", null), Key("Logical", "AK_CalculationEntity", "Id", null)]);

            // FormulaCalculation carries the outbound Foreign key to CalculationEntity (typed-body join).
            var formulaContainer = Container("FormulaCalculation", [],
            [Key("Foreign", "FK_FormulaCalculation_CalculationEntity", "CalculationEntityRowId", entityContainer)]);

            var containers = new List<IDataContainer>
            { entityContainer, inputContainer, stepContainer, fieldContainer, operandContainer, formulaContainer };

            var path = new Mock<IDataPath>();
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
            store.Setup(s => s.Name).Returns("ConfigurationDb");
            store.Setup(s => s.Paths).Returns(new List<IDataPath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "calc", StringComparison.Ordinal)
                    ? GenericResult<IDataPath>.Success(path.Object)
                    : GenericResult<IDataPath>.Failure(new GenericMessage("nf")));
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
            var field = new Mock<global::Fdw.Data.Abstractions.IDataField>();
            field.Setup(f => f.Name).Returns(fkColumn);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyName).Returns($"FK_{fkColumn}_{owner.Name}");
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            return new ReferencingKeyBinding(key.Object, owner);
        }

        private static IContainerKey Key(string keyType, string keyName, string localField, IDataContainer? referenced)
        {
            var field = new Mock<global::Fdw.Data.Abstractions.IDataField>();
            field.Setup(f => f.Name).Returns(localField);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            // Why: KeyType is the abstract KeyTypeBase TypeOption — use the real concrete instances so
            // FindForeignKey/FindKeyFieldName read the genuine Name ("Foreign"/"Physical"/"Logical").
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
    }
}
