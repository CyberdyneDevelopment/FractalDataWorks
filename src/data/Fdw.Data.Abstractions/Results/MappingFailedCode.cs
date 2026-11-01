using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A generated POCO mapper failed to map a value (90002 ConversionFailed). The runtime exception
/// detail is supplied via ResultDetails ({Type}/{Source}/{Error}).
/// </summary>
[TypeOption(typeof(MapperResultCodes), "MappingFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MappingFailedCode : MapperResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MappingFailedCode"/> class.
    /// </summary>
    public MappingFailedCode()
        : base(
            90002,
            "MappingFailed",
            ResultSeverities.ByName("Error"),
            "Failed to map {Type} from {Source}: {Error}",
            isRetryable: false)
    {
    }
}
