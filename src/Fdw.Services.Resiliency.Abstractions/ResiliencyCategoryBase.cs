using Fdw.Collections;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Base class for resiliency policy categories.
/// </summary>
public abstract class ResiliencyCategoryBase : TypeOptionBase<int, ResiliencyCategoryBase>, IResiliencyCategory
{
    /// <summary>
    /// Initializes a new instance of <see cref="ResiliencyCategoryBase"/>.
    /// </summary>
    protected ResiliencyCategoryBase(int id, string name) : base(id, name) { }
}
