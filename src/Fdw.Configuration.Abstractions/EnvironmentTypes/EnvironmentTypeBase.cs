using Fdw.Collections;

namespace Fdw.Configuration;

/// <summary>
/// Abstract base class for deployment environment types (Local, Dev, QA, Prod).
/// </summary>
public abstract class EnvironmentTypeBase : TypeOptionBase<int, EnvironmentTypeBase>, ITypeOption<int, EnvironmentTypeBase>, IEnvironmentType
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnvironmentTypeBase"/>.
    /// </summary>
    /// <param name="id">The numeric identifier for this environment type.</param>
    /// <param name="name">The name of this environment type.</param>
    protected EnvironmentTypeBase(int id, string name) : base(id, name)
    {
    }
}
