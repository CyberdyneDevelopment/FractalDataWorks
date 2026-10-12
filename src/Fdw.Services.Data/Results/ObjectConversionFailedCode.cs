using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Failed to convert object to dictionary for calculated fields.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ObjectConversionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ObjectConversionFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectConversionFailedCode"/> class.
    /// </summary>
    public ObjectConversionFailedCode()
        : base(91003, "ObjectConversionFailed", ResultSeverities.ByName("Error"),
            "Failed to convert object to dictionary",
            isRetryable: false)
    {
    }
}