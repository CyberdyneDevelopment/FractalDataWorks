using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Multitenancy.Abstractions.Scoping;

/// <summary>
/// Every dimension a row can be scoped by.
/// </summary>
/// <remarks>
/// A TypeCollection rather than fixed properties because nothing here has per-layer logic — a layer
/// is a value carried in a claim, stamped into session context, and compared against a column. That
/// is the test for which shape to use: a collection where the platform carries values it does not
/// interpret, a closed set where it branches per member.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ScopeLayerBase), typeof(IScopeLayer), typeof(ScopeLayers))]
public abstract partial class ScopeLayers : TypeCollectionBase<ScopeLayerBase, IScopeLayer>
{
}
