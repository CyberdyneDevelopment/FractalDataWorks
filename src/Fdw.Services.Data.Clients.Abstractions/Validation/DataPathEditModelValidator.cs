using FluentValidation;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Clients.Validation;

/// <summary>
/// Validator for <see cref="DataPathEditModel"/>.
/// </summary>
public sealed class DataPathEditModelValidator : AbstractValidator<DataPathEditModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPathEditModelValidator"/> class.
    /// </summary>
    public DataPathEditModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(128)
            .WithMessage("Name must not exceed 128 characters");

        RuleFor(x => x.PhysicalPath)
            .NotEmpty()
            .WithMessage("PhysicalPath is required")
            .MaximumLength(512)
            .WithMessage("PhysicalPath must not exceed 512 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters")
            .When(x => x.Description is not null);

        RuleForEach(x => x.Containers)
            .SetValidator(new ContainerEditModelValidator());
    }
}
