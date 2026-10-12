using Fdw.Collections;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Interface for resiliency policy categories.
/// </summary>
public interface IResiliencyCategory : ITypeOption<int, ResiliencyCategoryBase> { }
