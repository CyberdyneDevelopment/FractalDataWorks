using Fdw.Collections;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Base class for resiliency policy categories.
/// </summary>
public abstract class ResiliencyCategoryBase : TypeOptionBase<ResiliencyCategoryBase>, IResiliencyCategory
{
    /// <summary>
    /// Initializes a new instance of <see cref="ResiliencyCategoryBase"/>.
    /// </summary>
    protected ResiliencyCategoryBase(string name) : base(name) { }
}
