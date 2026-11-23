using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The ConfigurationRetire command is sent with POST as a GraphQL query and names no operation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpCommandVerbs), "ConfigurationRetire")]
public sealed class ConfigurationRetireHttpCommandVerb : HttpCommandVerbBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationRetireHttpCommandVerb"/> class.</summary>
    public ConfigurationRetireHttpCommandVerb()
        : base("ConfigurationRetire", "POST", null, null)
    {
    }
}
