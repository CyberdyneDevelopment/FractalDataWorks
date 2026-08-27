namespace Fdw.Configuration;

/// <summary>
/// One implementation's own configuration, owned by a domain row.
/// </summary>
/// <remarks>
/// Every <c>I&lt;Domain&gt;ImplementationConfiguration</c> implements this, which is what lets the
/// domain and implementation provider contracts constrain their configuration without naming a domain.
/// </remarks>
public interface IImplementationConfiguration : IGenericConfiguration
{
}
