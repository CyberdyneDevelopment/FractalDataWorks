using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="HostedToolKinds"/>: the kinds of tool a provider runs itself.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class HostedToolKindBase : TypeOptionBase<HostedToolKindBase>, IHostedToolKind
{
    /// <summary>Initializes a new instance of the <see cref="HostedToolKindBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="requiredCapability">The name of the <see cref="ModelCapabilities"/> option a model needs to use a tool of this kind.</param>
    protected HostedToolKindBase(string name, string requiredCapability) : base(name)
    {
        RequiredCapability = requiredCapability;
    }

    /// <inheritdoc />
    public string RequiredCapability { get; }
}
