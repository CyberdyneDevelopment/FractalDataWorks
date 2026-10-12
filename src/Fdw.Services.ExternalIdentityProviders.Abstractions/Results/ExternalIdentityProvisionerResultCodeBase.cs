using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Results;

/// <summary>
/// Base class for ExternalIdentityProvisioner result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ExternalIdentityProvisionerResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance for the Empty sentinel.</summary>
    protected ExternalIdentityProvisionerResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentityProvisionerResultCodeBase"/> class
    /// using the categorized-number identity.
    /// </summary>
    protected ExternalIdentityProvisionerResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "EIDP", isRetryable)
    {
    }
}
