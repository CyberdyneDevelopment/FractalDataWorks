using FluentValidation;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Clients.Validation;

/// <summary>
/// Validator for <see cref="ContainerEditModel"/>.
/// </summary>
public sealed class ContainerEditModelValidator : AbstractValidator<ContainerEditModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerEditModelValidator"/> class.
    /// </summary>
    public ContainerEditModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(128)
            .WithMessage("Name must not exceed 128 characters");

        RuleFor(x => x.PhysicalName)
            .NotEmpty()
            .WithMessage("PhysicalName is required")
            .MaximumLength(256)
            .WithMessage("PhysicalName must not exceed 256 characters");

        RuleFor(x => x.ContainerType)
            .NotEmpty()
            .WithMessage("ContainerType is required");
    }
}
