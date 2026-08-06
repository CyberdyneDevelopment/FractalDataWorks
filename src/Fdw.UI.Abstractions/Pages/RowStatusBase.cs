using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for row status for visual indicators.
/// </summary>
// Why: pure TypeOption base — trivial pass-through constructor, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class RowStatusBase : TypeOptionBase<int, RowStatusBase>, IRowStatus
{
    /// <summary>
    /// Initializes a new instance of <see cref="RowStatusBase"/>.
    /// </summary>
    protected RowStatusBase(int id, string name) : base(id, name) { }
}
