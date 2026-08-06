using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="GlossaryTermRelationConfiguration"/>.
/// </summary>
public sealed class GlossaryTermRelationConfigurationValidator : FdwConfigurationValidator<GlossaryTermRelationConfiguration>
{
    private static readonly string[] ValidRelationTypes = ["Synonym", "Antonym", "Related"];

    /// <summary>
    /// Initializes a new instance of the <see cref="GlossaryTermRelationConfigurationValidator"/> class.
    /// </summary>
    public GlossaryTermRelationConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.RelatedTermName)
            .NotEmpty()
            .WithMessage("RelatedTermName is required");

        RuleFor(x => x.RelationType)
            .Must(t => System.Array.IndexOf(ValidRelationTypes, t) >= 0)
            .WithMessage("RelationType must be one of: Synonym, Antonym, Related");
    }
}
