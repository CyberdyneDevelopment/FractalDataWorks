using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="ModelCapabilities"/>: what a configured model can carry.
/// </summary>
public interface IModelCapability : ITypeOption<int, ModelCapabilityBase>
{
}
