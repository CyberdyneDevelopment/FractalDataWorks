using Fdw.Collections;

namespace Fdw.Configuration;

/// <summary>
/// Abstract base class for deployment environment types (Local, Dev, QA, Prod).
/// </summary>
public abstract class EnvironmentTypeBase : TypeOptionBase<EnvironmentTypeBase>, ITypeOption<int, EnvironmentTypeBase>, IEnvironmentType
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnvironmentTypeBase"/>.
    /// </summary>
    /// <param name="name">The name of this environment type.</param>
    protected EnvironmentTypeBase(string name) : base(name)
    {
    }
}
