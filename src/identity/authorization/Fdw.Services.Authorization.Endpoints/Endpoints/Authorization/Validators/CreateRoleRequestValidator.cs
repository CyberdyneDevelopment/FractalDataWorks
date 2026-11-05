using FluentValidation;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Validation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Authorization.Endpoints.Validators;

/// <summary>
/// The rules a create-role request must meet in any host. A host derives from this class (so the endpoint scan finds a concrete
/// validator) and adds only what the host alone knows (FDW-3572).
/// </summary>
/// <remarks>
/// The rules are the role configuration validator's (<see cref="RoleConfigurationValidator"/>), taken through <c>RulesOf</c>.
/// </remarks>
public abstract class CreateRoleRequestValidator : FdwEndpointValidator<CreateRoleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CreateRoleRequestValidator"/> class.</summary>
    protected CreateRoleRequestValidator()
    {
        // Why nothing else here: every rule about the role's name, display name and description is the role configuration validator's, so the
        // request answers 400 for exactly what the save check would refuse (FDW-3884). A rule only a request states would be written here.
        RulesOf(request => new RoleImplementationConfiguration
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
        });

        // The request-only rule: whether the role is tenant scoped is the caller's choice and has no default (FDW-3823).
        RuleFor(x => x.IsTenantScoped)
            .NotNull()
            .WithMessage("IsTenantScoped is required");
    }
}
