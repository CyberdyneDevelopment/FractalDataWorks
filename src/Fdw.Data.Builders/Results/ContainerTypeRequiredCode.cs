using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Container type is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ContainerTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerTypeRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerTypeRequiredCode"/> class.
    /// </summary>
    public ContainerTypeRequiredCode()
        : base(21024, "ContainerTypeRequired",
            ResultSeverities.ByName("Error"),
            "Container type is required",
            isRetryable: false)
    {
    }
}