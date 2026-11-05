using Fdw.Services.Tags.Abstractions;
using FluentValidation;
using Fdw.Services.Tags.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Tags.Validators;

/// <summary>Validator for <see cref="TagImplementationConfiguration"/>: a tag has a name, one of the three owner kinds, and the Id of its owner.</summary>
/// <remarks>The length of the name and the uniqueness across the owner chain are checked by the Tags service, which can see the stored tags.</remarks>
public sealed class TagConfigurationValidator : FdwConfigurationValidator<TagImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TagConfigurationValidator"/> class.</summary>
    public TagConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.OwnerKind)
            .Must(kind => kind is { Length: > 0 } && !ReferenceEquals(TagOwnerKinds.ByName(kind), TagOwnerKinds.NotFound))
            .WithMessage("OwnerKind must be one of the TagOwnerKinds");

        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .WithMessage("OwnerId is required");
    }
}
