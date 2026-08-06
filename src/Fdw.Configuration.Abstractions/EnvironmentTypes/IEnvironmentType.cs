using Fdw.Collections;

namespace Fdw.Configuration;

/// <summary>
/// Marker interface for deployment environment type options.
/// </summary>
public interface IEnvironmentType : ITypeOption<int, EnvironmentTypeBase>
{
}
