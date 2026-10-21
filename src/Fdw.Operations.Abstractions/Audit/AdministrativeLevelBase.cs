using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// Base class for the levels of administration, using the CRTP pattern.
/// </summary>
public abstract class AdministrativeLevelBase : TypeOptionBase<AdministrativeLevelBase>, IAdministrativeLevel
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected AdministrativeLevelBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown administrative level", null)
    {
        Depth = -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrativeLevelBase"/> class.
    /// </summary>
    /// <param name="name">Name of the level (must match the TypeOption attribute).</param>
    /// <param name="depth">How far down the tree the level sits (0 = platform).</param>
    protected AdministrativeLevelBase(string name, int depth)
        : base(name, $"TypeOptions:{name}", name, $"Administrative level: {name}", null)
    {
        Depth = depth;
    }

    /// <inheritdoc />
    public int Depth { get; }
}
