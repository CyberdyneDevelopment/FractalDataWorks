using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 6 (60000–69999): configuration / setup — missing, unconfigured, or unregistered setup.
/// </summary>
[TypeOption(typeof(ResultCategories), "Configuration", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationCategory"/> class.
    /// </summary>
    public ConfigurationCategory()
        : base(id: 6, name: "Configuration", isFailure: true, isRetryable: false, httpStatus: 500, clientMessage: "A service configuration error occurred", clientAction: "Contact your administrator")
    {
    }
}
