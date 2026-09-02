using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Universes.Results;

/// <summary>
/// A member, resource or relationship named for a narrow write is not in that universe.
/// </summary>
/// <remarks>
/// Distinct from the universe itself being absent: the project exists and the row does not, which
/// usually means someone else removed it between the caller reading the map and writing to it.
/// HTTP 404.
/// </remarks>
[TypeOption(typeof(UniversesResultCodes), "UniverseChildNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UniverseChildNotFoundCode : UniversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniverseChildNotFoundCode"/> class.</summary>
    public UniverseChildNotFoundCode()
        : base(30000, "UniverseChildNotFound",
            ResultSeverities.ByName("Error"),
            "Universe '{name}' has no {kind} with id '{id}'",
            isRetryable: false)
    {
    }
}
