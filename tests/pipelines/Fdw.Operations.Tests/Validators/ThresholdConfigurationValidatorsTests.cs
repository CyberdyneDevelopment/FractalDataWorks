using Fdw.Operations.Configuration;
using Fdw.Operations.Validators;

namespace Fdw.Operations.Tests.Validators;

/// <summary>Tests for the validators of the threshold configurations (FDW-4046): a required field left out is refused, and a name the collections do not hold is refused.</summary>
public sealed class ThresholdConfigurationValidatorsTests
{
    private static ThresholdFilterConditionConfiguration Condition() =>
        new() { Id = Guid.NewGuid(), Ordinal = 1, FieldName = "consecutiveFailures", Operator = "GreaterThanOrEqual", Value = "3", DataType = "Int64" };

    private static ThresholdFilterGroupConfiguration Group() =>
        new() { Id = Guid.NewGuid(), Ordinal = 1, Operator = "All", Conditions = [Condition()] };

    private static ThresholdRuleConfiguration Rule() =>
        new() { Id = Guid.NewGuid(), Ordinal = 1, Escalation = "Alert", EscalationPolicy = "Standard", FilterGroups = [Group()] };

    private static ThresholdImplementationConfiguration Threshold() =>
        new() { Id = Guid.NewGuid(), Name = "FailureRun", Implementation = "PipelineFailures", SubjectKind = "Pipeline", Rules = [Rule()] };

    private static ThresholdParameterConfiguration Parameter() =>
        new() { Id = Guid.NewGuid(), Name = "FieldName", Value = "Age", DataType = "String", Ordinal = 1 };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ACompleteThresholdWithItsRulesAndParametersPasses()
    {
        var threshold = Threshold();
        threshold.Parameters = [Parameter()];

        new ThresholdImplementationConfigurationValidator().Validate(threshold).IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("", "PipelineFailures", "Pipeline", "Name")]
    [InlineData("FailureRun", "", "Pipeline", "Implementation")]
    [InlineData("FailureRun", "PipelineFailures", "", "SubjectKind")]
    [InlineData("FailureRun", "PipelineFailures", "Galaxy", "SubjectKind")]
    public void AThresholdWithoutANameATypeOrAKnownSubjectKindIsRefused(string name, string implementation, string kind, string property)
    {
        var threshold = Threshold();
        threshold.Name = name;
        threshold.Implementation = implementation;
        threshold.SubjectKind = kind;

        var result = new ThresholdImplementationConfigurationValidator().Validate(threshold);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == property);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RulesAndParametersThatRepeatAnOrdinalAreRefused()
    {
        var threshold = Threshold();
        threshold.Rules = [Rule(), Rule()];
        threshold.Parameters = [Parameter(), Parameter()];

        var result = new ThresholdImplementationConfigurationValidator().Validate(threshold);

        result.Errors.ShouldContain(error => error.PropertyName == "Rules");
        result.Errors.ShouldContain(error => error.PropertyName == "Parameters");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARuleBelowAThresholdIsCheckedWhenTheThresholdIsWritten()
    {
        var threshold = Threshold();
        threshold.Rules[0].Escalation = "Nothing";

        new ThresholdImplementationConfigurationValidator().Validate(threshold).Errors.ShouldContain(error => error.PropertyName == "Rules[0].Escalation");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARuleNeedsAnEscalationThatExistsAndWhatItNeeds()
    {
        var validator = new ThresholdRuleConfigurationValidator();
        var noEscalation = Rule();
        noEscalation.Escalation = string.Empty;
        var unknown = Rule();
        unknown.Escalation = "Nothing";
        var alertWithoutPolicy = Rule();
        alertWithoutPolicy.EscalationPolicy = null;
        var notifyWithoutRules = Rule();
        notifyWithoutRules.Escalation = "Notify";

        validator.Validate(Rule()).IsValid.ShouldBeTrue();
        validator.Validate(noEscalation).Errors.ShouldContain(error => error.PropertyName == "Escalation");
        validator.Validate(unknown).Errors.ShouldContain(error => error.PropertyName == "Escalation");
        validator.Validate(alertWithoutPolicy).Errors.ShouldContain(error => error.PropertyName == "EscalationPolicy");
        validator.Validate(notifyWithoutRules).Errors.ShouldContain(error => error.PropertyName == "Notifications");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AFilterGroupNeedsAllOrAnyAndARealParent()
    {
        var validator = new ThresholdFilterGroupConfigurationValidator();
        var noOperator = Group();
        noOperator.Operator = string.Empty;
        var other = Group();
        other.Operator = "Some";
        var emptyParent = Group();
        emptyParent.ParentGroupId = Guid.Empty;

        validator.Validate(Group()).IsValid.ShouldBeTrue();
        validator.Validate(noOperator).Errors.ShouldContain(error => error.PropertyName == "Operator");
        validator.Validate(other).Errors.ShouldContain(error => error.PropertyName == "Operator");
        validator.Validate(emptyParent).Errors.ShouldContain(error => error.PropertyName == "ParentGroupId");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("", "GreaterThan", "3", "Int64", "FieldName")]
    [InlineData("rejected", "", "3", "Int64", "Operator")]
    [InlineData("rejected", "Nearly", "3", "Int64", "Operator")]
    [InlineData("rejected", "In", "3", "Int64", "Operator")]
    [InlineData("rejected", "GreaterThan", "3", "", "DataType")]
    [InlineData("rejected", "GreaterThan", "3", "Decimal", "DataType")]
    [InlineData("rejected", "GreaterThan", null, "Int64", "Value")]
    [InlineData("rejected", "GreaterThan", "many", "Int64", "Value")]
    public void AConditionWithoutAFieldAKnownOperatorADataTypeAndAReadableValueIsRefused(string field, string op, string? value, string dataType, string property)
    {
        var condition = Condition();
        condition.FieldName = field;
        condition.Operator = op;
        condition.Value = value;
        condition.DataType = dataType;

        new ThresholdFilterConditionConfigurationValidator().Validate(condition).Errors.ShouldContain(error => error.PropertyName == property);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ACompleteConditionPasses()
    {
        new ThresholdFilterConditionConfigurationValidator().Validate(Condition()).IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ANotificationNeedsTheNameOfTheRuleItSends()
    {
        var validator = new ThresholdRuleNotificationConfigurationValidator();

        validator.Validate(new ThresholdRuleNotificationConfiguration { Id = Guid.NewGuid(), NotificationRuleName = "OnCall", Ordinal = 1 }).IsValid.ShouldBeTrue();
        validator.Validate(new ThresholdRuleNotificationConfiguration { Id = Guid.NewGuid(), NotificationRuleName = string.Empty, Ordinal = 1 }).Errors
            .ShouldContain(error => error.PropertyName == "NotificationRuleName");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("", "Age", "String", "Name")]
    [InlineData("FieldName", "Age", "", "DataType")]
    [InlineData("MinValue", "1", "Money", "DataType")]
    [InlineData("MinValue", null, "Double", "Value")]
    [InlineData("MinValue", "low", "Double", "Value")]
    public void AParameterWithoutANameADataTypeAndAReadableValueIsRefused(string name, string? value, string dataType, string property)
    {
        var parameter = Parameter();
        parameter.Name = name;
        parameter.Value = value;
        parameter.DataType = dataType;

        new ThresholdParameterConfigurationValidator().Validate(parameter).Errors.ShouldContain(error => error.PropertyName == property);
    }
}
