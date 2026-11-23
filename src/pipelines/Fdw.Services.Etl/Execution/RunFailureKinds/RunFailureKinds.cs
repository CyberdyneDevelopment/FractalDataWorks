using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The kinds of failure that ended a pipeline run: classified and counted, classified and refused, unclassified and refused, and
/// unclassified, each telling itself in words.
/// </summary>
/// <remarks>A failure's kind is the option whose <c>Classified</c> and <c>Refused</c> are the failure's; the four hold every pair.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RunFailureKindBase), typeof(IRunFailureKind), typeof(RunFailureKinds))]
public abstract partial class RunFailureKinds : TypeCollectionBase<RunFailureKindBase, IRunFailureKind>
{
    /// <summary>Finds the kind of a failure.</summary>
    /// <param name="classified">Whether the failure carries a coded failure message in a known category.</param>
    /// <param name="refused">Whether the failure does not count toward a failure threshold.</param>
    /// <returns>The kind that holds the pair.</returns>
    public static IRunFailureKind Of(bool classified, bool refused)
        => All().First(kind => kind.Classified == classified && kind.Refused == refused);
}
