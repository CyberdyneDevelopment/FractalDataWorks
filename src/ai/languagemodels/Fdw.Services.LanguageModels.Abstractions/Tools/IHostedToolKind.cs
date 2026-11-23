using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="HostedToolKinds"/>: the kinds of tool a provider runs itself.
/// </summary>
public interface IHostedToolKind : ITypeOption<int, HostedToolKindBase>
{

    /// <summary>Gets the name of the <see cref="ModelCapabilities"/> option a model needs to use a tool of this kind.</summary>
    string RequiredCapability { get; }
}
