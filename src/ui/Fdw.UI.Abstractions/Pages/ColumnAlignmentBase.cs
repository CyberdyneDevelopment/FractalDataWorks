using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for column text alignment.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ColumnAlignmentBase : TypeOptionBase<ColumnAlignmentBase>, IColumnAlignment
{
    /// <summary>
    /// Initializes a new instance of <see cref="ColumnAlignmentBase"/>.
    /// </summary>
    protected ColumnAlignmentBase(string name) : base(name) { }
}
