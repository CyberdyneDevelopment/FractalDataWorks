using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for activity severity levels.
/// </summary>
// Why: pure TypeOption base — trivial pass-through constructor, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class ActivitySeverityBase : TypeOptionBase<int, ActivitySeverityBase>, IActivitySeverity
{
    /// <summary>
    /// Initializes a new instance of <see cref="ActivitySeverityBase"/>.
    /// </summary>
    protected ActivitySeverityBase(int id, string name) : base(id, name) { }
}
