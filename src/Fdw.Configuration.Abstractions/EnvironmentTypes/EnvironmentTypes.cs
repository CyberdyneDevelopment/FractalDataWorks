using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Configuration;

/// <summary>
/// TypeCollection of deployment environment types: Local, Dev, QA, Prod.
/// Used as a <c>[ValuesFrom]</c> source on configuration properties to drive dropdown population.
/// </summary>
/// <example>
/// <code>
/// [ValuesFrom(typeof(EnvironmentTypes))]
/// public string? Environment { get; set; }
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EnvironmentTypeBase), typeof(IEnvironmentType), typeof(EnvironmentTypes))]
public abstract partial class EnvironmentTypes : TypeCollectionBase<EnvironmentTypeBase, IEnvironmentType>
{
    // Source generator creates: Local, Dev, QA, Prod static properties
    // plus All(), ByName(), ById(), NotFound()
}
