using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>
/// The kinds of resource that can be attached to a universe.
/// </summary>
/// <remarks>
/// <para>
/// This collection gathers options that attach themselves. The package owning a resource declares
/// its own kind — <c>Fdw.Data.DataSets</c> declares the data set kind, the pipelines package will
/// declare its own — so the set a host understands is exactly the set of domains it has
/// referenced. A host with no pipeline packages cannot attach a pipeline to a universe, which is
/// correct rather than unfortunate.
/// </para>
/// <para>
/// This is why <c>universe.UniverseResource.ResourceType</c> carries no CHECK constraint. A closed
/// database constraint would have to be widened by a schema change every time a domain was added,
/// and would encode a set the database has no way to know.
/// </para>
/// <para>
/// Id allocation. Options are declared by the packages owning their resources, so this table is
/// the one place a new domain looks to pick a free number. A duplicate fails loud when the
/// collection freezes rather than one option silently winning the lookup.
/// </para>
/// <list type="table">
/// <item><description>1  DataSet — <c>Fdw.Data.DataSets</c></description></item>
/// <item><description>2  DataStore</description></item>
/// <item><description>3  Connection</description></item>
/// <item><description>4  Pipeline</description></item>
/// <item><description>5  OrchestrationNode</description></item>
/// <item><description>6  Schedule</description></item>
/// <item><description>7  Calculation</description></item>
/// <item><description>8  Transform</description></item>
/// <item><description>9  Expectation</description></item>
/// <item><description>10 EscalationPolicy</description></item>
/// <item><description>11 GlossaryTerm</description></item>
/// <item><description>12 Snapshot</description></item>
/// <item><description>13 SavedView — <c>Fdw.Services.Universes.Abstractions</c></description></item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UniverseResourceKindBase), typeof(IUniverseResourceKind), typeof(UniverseResourceKinds))]
public abstract partial class UniverseResourceKinds
    : TypeCollectionBase<UniverseResourceKindBase, IUniverseResourceKind>
{
    /// <summary>Gets the service category this collection belongs to.</summary>
    public static string ServiceCategory => "Universe";
}
