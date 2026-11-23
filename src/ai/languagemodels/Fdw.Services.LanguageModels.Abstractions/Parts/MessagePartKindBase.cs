using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="MessagePartKinds"/>: the kinds of part a message is made of.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class MessagePartKindBase : TypeOptionBase<MessagePartKindBase>, IMessagePartKind
{
    /// <summary>Initializes a new instance of the <see cref="MessagePartKindBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="requiredCapability">The name of the <see cref="ModelCapabilities"/> option a model needs to carry a part of this kind.</param>
    protected MessagePartKindBase(string name, string requiredCapability) : base(name)
    {
        RequiredCapability = requiredCapability;
    }

    /// <inheritdoc />
    public string RequiredCapability { get; }
}
