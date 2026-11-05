using FluentValidation;
using Fdw.Validation;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// Validator for one field mapping of a Map transformation (<see cref="MapTransformationFieldMappingConfiguration"/>).
/// </summary>
/// <remarks>
/// FDW-3823: whether a mapping runs is the caller's choice, so a mapping that leaves it out is refused rather than bound to false.
/// </remarks>
public sealed class MapTransformationFieldMappingConfigurationValidator : FdwConfigurationValidator<MapTransformationFieldMappingConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapTransformationFieldMappingConfigurationValidator"/> class.
    /// </summary>
    public MapTransformationFieldMappingConfigurationValidator()
    {
        RuleFor(x => x.IsEnabled)
            .NotNull()
            .WithMessage("IsEnabled is required: whoever builds a field mapping states whether it runs");
    }
}
