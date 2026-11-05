using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.RoslynWorkspace;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.RoslynWorkspace.Validation;

/// <summary>States that <see cref="RoslynWorkspaceConnectionConfiguration"/> is checked by <see cref="RoslynWorkspaceConnectionConfigurationValidator"/> when it is written (FDW-3541).</summary>
/// <remarks>The connection endpoints run the same validator on create and update through the connection type; this runs it again on any write through the domain provider.</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "RoslynWorkspaceConnectionConfiguration")]
public sealed class RoslynWorkspaceConnectionConfigurationValidation : ValidatedConfiguration<RoslynWorkspaceConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="RoslynWorkspaceConnectionConfigurationValidation"/> class.</summary>
    public RoslynWorkspaceConnectionConfigurationValidation()
        : base("RoslynWorkspaceConnectionConfiguration", new RoslynWorkspaceConnectionConfigurationValidator())
    {
    }
}
