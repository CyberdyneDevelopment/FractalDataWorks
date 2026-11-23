using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The ConfigurationSave command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "ConfigurationSave")]
public sealed class ConfigurationSaveHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationSaveHttpCommandVerb"/> class.</summary>
    public ConfigurationSaveHttpCommandVerb()
        : base("ConfigurationSave", "POST", null, null)
    {
    }
}
