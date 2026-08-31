using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Universes.Results;

/// <summary>
/// A read succeeded but carried no universe, after existence had already been established.
/// </summary>
/// <remarks>
/// This is an internal inconsistency, not a missing row — a caller asking for something that does
/// not exist gets a not-found path instead. It exists so that case fails loudly with its own code
/// rather than being absorbed by an empty list or a substituted record. HTTP 500.
/// </remarks>
[TypeOption(typeof(UniversesResultCodes), "UniverseLoadReturnedNoValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UniverseLoadReturnedNoValueCode : UniversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniverseLoadReturnedNoValueCode"/> class.</summary>
    public UniverseLoadReturnedNoValueCode()
        : base(90000, "UniverseLoadReturnedNoValue",
            ResultSeverities.ByName("Error"),
            "Universe '{name}' reported success but returned no value",
            isRetryable: false)
    {
    }
}
