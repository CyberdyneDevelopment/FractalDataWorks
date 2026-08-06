using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.RoslynWorkspace.Validation;

/// <summary>
/// Validator for <see cref="RoslynWorkspaceConnectionConfiguration"/>.
/// </summary>
public sealed class RoslynWorkspaceConnectionConfigurationValidator : FdwConfigurationValidator<RoslynWorkspaceConnectionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceConnectionConfigurationValidator"/> class.
    /// </summary>
    public RoslynWorkspaceConnectionConfigurationValidator()
    {
        // Why: Name is a header field on ConnectionConfiguration after config-split.
        // RoslynWorkspaceConnectionConfiguration exposes it as an explicit IGenericConfiguration
        // member returning string.Empty — it cannot be validated here.

        RuleFor(x => x.SolutionPath)
            .NotEmpty()
            .WithMessage("SolutionPath is required");

        RuleFor(x => x.ModeName)
            .NotEmpty()
            .WithMessage("ModeName is required");
    }
}
