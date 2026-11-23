using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="MessagePartKinds"/>: the kinds of part a message is made of.
/// </summary>
public interface IMessagePartKind : ITypeOption<int, MessagePartKindBase>
{

    /// <summary>Gets the name of the <see cref="ModelCapabilities"/> option a model needs to carry a part of this kind.</summary>
    string RequiredCapability { get; }
}
