using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>The three registration phases, tracked independently by <see cref="Fdw.ServiceTypes.ServiceTypePhaseState"/> (FDW-4149). Replaces the Phase enum.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ServiceTypePhaseBase), typeof(IServiceTypePhase), typeof(ServiceTypePhases))]
public abstract partial class ServiceTypePhases : TypeCollectionBase<ServiceTypePhaseBase, IServiceTypePhase>
{
}
