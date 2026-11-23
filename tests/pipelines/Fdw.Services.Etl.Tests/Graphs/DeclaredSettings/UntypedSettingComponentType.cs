using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Tests.Graphs.DeclaredSettings;

/// <summary>A component type that declares a setting no column holds and states no data type for it (FDW-6133); built directly, not registered.</summary>
internal sealed class UntypedSettingComponentType : ComponentTypeBase
{
    public UntypedSettingComponentType()
        : base("Untyped", "Untyped", "Declares a setting with no data type.", "Control flow", holdsComponents: false, [new StageMember("mood", "Text", true, "A mood.")])
    {
    }
}
