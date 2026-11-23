using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Tests.Graphs.Builders;

/// <summary>An action component type from another package (FDW-6240), built directly and not registered; its name is chosen by each test.</summary>
internal sealed class LoneComponentType : ComponentTypeBase
{
    public LoneComponentType(string name)
        : base(name, name, "An action with no setting.", "Control flow", holdsComponents: false, [])
    {
    }
}
