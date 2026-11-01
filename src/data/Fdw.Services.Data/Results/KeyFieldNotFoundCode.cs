using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Key field was not found in field definitions.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "KeyFieldNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class KeyFieldNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyFieldNotFoundCode"/> class.
    /// </summary>
    public KeyFieldNotFoundCode()
        : base(31007, "KeyFieldNotFound", ResultSeverities.ByName("Error"),
            "Key field '{KeyField}' not found in field definitions",
            isRetryable: false)
    {
    }
}