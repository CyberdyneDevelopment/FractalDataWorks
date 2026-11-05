using Fdw.Collections;

namespace Fdw.Services.Abstractions;

/// <summary>One of the three registration phases a service type or collection runs: Configure, Register or Initialize (FDW-4149).</summary>
public interface IServiceTypePhase : ITypeOption<int, IServiceTypePhase>
{
}
