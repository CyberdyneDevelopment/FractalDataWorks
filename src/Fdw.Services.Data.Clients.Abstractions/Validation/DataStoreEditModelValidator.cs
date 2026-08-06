using FluentValidation;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Clients.Validation;

/// <summary>
/// Validator for <see cref="DataStoreEditModel"/>.
/// </summary>
public sealed class DataStoreEditModelValidator : AbstractValidator<DataStoreEditModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreEditModelValidator"/> class.
    /// </summary>
    public DataStoreEditModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(3)
            .WithMessage("Name must be at least 3 characters")
            .MaximumLength(128)
            .WithMessage("Name must not exceed 128 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage("Name must start with a letter and contain only letters, digits, or underscores");

        RuleFor(x => x.DisplayName)
            .MaximumLength(256)
            .WithMessage("DisplayName must not exceed 256 characters")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.ConnectionName)
            .NotEmpty()
            .WithMessage("ConnectionName is required");

        RuleFor(x => x.StoreType)
            .NotEmpty()
            .WithMessage("StoreType is required");

        RuleForEach(x => x.Paths)
            .SetValidator(new DataPathEditModelValidator());
    }
}
