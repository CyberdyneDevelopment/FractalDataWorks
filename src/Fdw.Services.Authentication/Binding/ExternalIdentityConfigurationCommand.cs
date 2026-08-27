using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authentication.Binding;

/// <summary>
/// Reads and writes <c>auth.ExternalIdentity</c>.
/// </summary>
/// <remarks>
/// The option is what claims the type for the write cascade. Without one, saving a configuration
/// that declares an external identity returns NoChildCommandForType and the row silently never
/// appears.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "ExternalIdentity")]
public sealed class ExternalIdentityConfigurationCommand : ConfigurationCommandBase<ExternalIdentityConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityConfigurationCommand"/> class.</summary>
    public ExternalIdentityConfigurationCommand() : base("ExternalIdentity") { }
}
