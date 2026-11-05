using System;
using System.Linq;
using FluentValidation;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Validation;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="ThresholdImplementationConfiguration"/> (FDW-4046): a threshold names itself, its type and its subject kind, and its rules and parameters are each valid with ordinals that do not repeat.
/// </summary>
public sealed class ThresholdImplementationConfigurationValidator : FdwConfigurationValidator<ThresholdImplementationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThresholdImplementationConfigurationValidator"/> class.
    /// </summary>
    public ThresholdImplementationConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(256)
            .WithMessage("Name must not exceed 256 characters");

        RuleFor(x => x.Implementation)
            .NotEmpty()
            .WithMessage("Implementation is required")
            .MaximumLength(100)
            .WithMessage("Implementation must not exceed 100 characters");

        RuleFor(x => x.SubjectKind)
            .NotEmpty()
            .WithMessage("SubjectKind is required")
            .Must(kind => string.IsNullOrEmpty(kind) || !ReferenceEquals(ThresholdSubjectKinds.ByName(kind), ThresholdSubjectKinds.NotFound))
            .WithMessage(x => $"SubjectKind '{x.SubjectKind}' is not a threshold subject kind");

        RuleFor(x => x.Rules)
            .Must(rules => rules.Select(rule => rule.Ordinal).Distinct().Count() == rules.Count)
            .WithMessage("The Ordinal of each rule must differ from the others");

        RuleForEach(x => x.Rules).SetValidator(new ThresholdRuleConfigurationValidator());

        RuleFor(x => x.Parameters)
            .Must(parameters => parameters.GroupBy(parameter => parameter.Name, StringComparer.Ordinal).All(named => named.Select(parameter => parameter.Ordinal).Distinct().Count() == named.Count()))
            .WithMessage("The Ordinal of each value of a parameter must differ from the others");

        RuleForEach(x => x.Parameters).SetValidator(new ThresholdParameterConfigurationValidator());
    }
}
