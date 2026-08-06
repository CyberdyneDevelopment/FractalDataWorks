using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Authorization.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateRoleRequest"/>.
/// </summary>
public abstract class CreateRoleRequestValidator : FdwEndpointValidator<CreateRoleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoleRequestValidator"/> class.
    /// </summary>
    protected CreateRoleRequestValidator()
    {
        ValidateName(x => x.Name, maxLength: 100);

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .MaximumLength(200)
                .WithMessage("DisplayName must not exceed 200 characters");
        });

        // Why: authz.Role.Description is nvarchar(500). 1000 let an over-long description past
        // validation only to fail at the database.
        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(500)
                .WithMessage("Description must not exceed 500 characters");
        });
    }
}
