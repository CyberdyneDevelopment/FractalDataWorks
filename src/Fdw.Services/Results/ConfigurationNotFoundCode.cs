using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Configuration not found for the specified identifier.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "ConfigurationNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationNotFoundCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationNotFoundCode"/> class.
    /// </summary>
    public ConfigurationNotFoundCode()
        : base(30000, "ConfigurationNotFound",
            ResultSeverities.ByName("Error"),
            "Configuration not found: '{Identifier}'",
            isRetryable: false)
    {
    }
}