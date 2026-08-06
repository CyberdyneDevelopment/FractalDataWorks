using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Components.Services;

/// <summary>
/// Base class for semantic status variants for status badges.
/// </summary>
// Why: pure TypeOption base — trivial pass-through constructor, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class StatusVariantBase : TypeOptionBase<int, StatusVariantBase>, IStatusVariant
{
    /// <summary>
    /// Initializes a new instance of <see cref="StatusVariantBase"/>.
    /// </summary>
    protected StatusVariantBase(int id, string name) : base(id, name) { }
}
