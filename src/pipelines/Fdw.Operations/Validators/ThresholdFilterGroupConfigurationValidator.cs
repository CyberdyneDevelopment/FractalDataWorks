using System;
using System.Linq;
using FluentValidation;
using Fdw.Operations.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Validation;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="ThresholdFilterGroupConfiguration"/> (FDW-4046): a group combines its conditions with All or Any, its parent is a real group when it states one, and its conditions are each valid with ordinals that do not repeat.
/// </summary>
public sealed class ThresholdFilterGroupConfigurationValidator : FdwConfigurationValidator<ThresholdFilterGroupConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThresholdFilterGroupConfigurationValidator"/> class.
    /// </summary>
    public ThresholdFilterGroupConfigurationValidator()
    {
        RuleFor(x => x.Operator)
            .NotEmpty()
            .WithMessage("Operator is required")
            .Must(logical => string.IsNullOrEmpty(logical) || string.Equals(logical, ThresholdCriteriaReader.AllOperator, StringComparison.Ordinal) || string.Equals(logical, ThresholdCriteriaReader.AnyOperator, StringComparison.Ordinal))
            .WithMessage(x => $"A filter group has the operator '{x.Operator}', which is neither {ThresholdCriteriaReader.AllOperator} nor {ThresholdCriteriaReader.AnyOperator}");

        RuleFor(x => x.ParentGroupId)
            .NotEqual(Guid.Empty)
            .WithMessage("ParentGroupId must name a group when it is stated");

        RuleFor(x => x.Conditions)
            .Must(conditions => conditions.Select(condition => condition.Ordinal).Distinct().Count() == conditions.Count)
            .WithMessage("The Ordinal of each condition must differ from the others");

        RuleForEach(x => x.Conditions).SetValidator(new ThresholdFilterConditionConfigurationValidator());
    }
}
