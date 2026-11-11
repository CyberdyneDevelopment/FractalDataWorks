using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>The inheritance chain refers back to itself (for example an explicit <c>&lt;inheritdoc cref="X"/&gt;</c> pointing at the documented member), so Roslyn cannot resolve it.</summary>
[TypeOption(typeof(UnresolvedReasons), "CircularInheritDoc")]
[ExcludeFromCodeCoverage]
public sealed class CircularInheritDocUnresolvedReason : UnresolvedReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="CircularInheritDocUnresolvedReason"/> class.</summary>
    public CircularInheritDocUnresolvedReason() : base(3, "CircularInheritDoc") { }
}
