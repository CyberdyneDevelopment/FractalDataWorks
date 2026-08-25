using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Results;
using TypedValidationResult = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleResult;

namespace Fdw.Orchestration.Pipelines.Abstractions.Tests;

public class ValidationRuleTypeBaseTests
{
    private sealed class TestValidationRuleType : ValidationRuleTypeBase
    {
        public TestValidationRuleType(
            int id,
            string name,
            bool requiresFields,
            bool supportsMultipleFields,
            bool requiresParameters = false,
            IReadOnlyList<string>? requiredParameterNames = null)
            : base(id, name, requiresFields, supportsMultipleFields, requiresParameters, requiredParameterNames)
        {
        }

        public override Task<IGenericResult<TypedValidationResult>> Validate(
            IReadOnlyDictionary<string, object?> record,
            IReadOnlyList<string> fields,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                GenericResult<TypedValidationResult>.Success(TypedValidationResult.Success()));
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsIdAndName()
    {
        var sut = new TestValidationRuleType(1, "NotNull",
            requiresFields: true,
            supportsMultipleFields: false);

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("NotNull");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsRequiresFields()
    {
        var sut = new TestValidationRuleType(1, "NotNull",
            requiresFields: true,
            supportsMultipleFields: false);

        sut.RequiresFields.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsSupportsMultipleFields()
    {
        var sut = new TestValidationRuleType(2, "CrossFieldCheck",
            requiresFields: true,
            supportsMultipleFields: true);

        sut.SupportsMultipleFields.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RequiresParametersDefaultsToFalse()
    {
        var sut = new TestValidationRuleType(1, "NotNull",
            requiresFields: true,
            supportsMultipleFields: false);

        sut.RequiresParameters.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RequiresParametersCanBeSetToTrue()
    {
        var sut = new TestValidationRuleType(3, "Range",
            requiresFields: true,
            supportsMultipleFields: false,
            requiresParameters: true);

        sut.RequiresParameters.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RequiredParameterNamesDefaultsToEmpty()
    {
        var sut = new TestValidationRuleType(1, "NotNull",
            requiresFields: true,
            supportsMultipleFields: false);

        sut.RequiredParameterNames.ShouldNotBeNull();
        sut.RequiredParameterNames.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RequiredParameterNamesCanBeSpecified()
    {
        var paramNames = new List<string> { "Min", "Max" };
        var sut = new TestValidationRuleType(3, "Range",
            requiresFields: true,
            supportsMultipleFields: false,
            requiresParameters: true,
            requiredParameterNames: paramNames);

        sut.RequiredParameterNames.Count.ShouldBe(2);
        sut.RequiredParameterNames[0].ShouldBe("Min");
        sut.RequiredParameterNames[1].ShouldBe("Max");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateCanBeInvoked()
    {
        var sut = new TestValidationRuleType(1, "NotNull",
            requiresFields: true,
            supportsMultipleFields: false);

        var record = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Name"] = "test" };
        var fields = new List<string> { "Name" };
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        var result = await sut.Validate(record, fields, parameters, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsValid.ShouldBeTrue();
    }
}
