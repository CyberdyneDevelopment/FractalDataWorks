using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Tests.Graphs.Contributions;

/// <summary>A contribution whose own dependency the host did not register (FDW-6240).</summary>
public sealed class UnresolvableContribution : IFlowBuildContribution
{
    public UnresolvableContribution(IUnregisteredService service)
    {
        Service = service;
    }

    public IUnregisteredService Service { get; }
}
