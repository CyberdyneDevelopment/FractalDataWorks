using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Components.Services;

/// <summary>
/// Base class for semantic status colors. Consumers map these to their styling framework.
/// </summary>
// Why: pure TypeOption base — trivial pass-through constructor, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class StatusColorBase : TypeOptionBase<int, StatusColorBase>, IStatusColor
{
    /// <summary>
    /// Initializes a new instance of <see cref="StatusColorBase"/>.
    /// </summary>
    protected StatusColorBase(int id, string name) : base(id, name) { }
}
