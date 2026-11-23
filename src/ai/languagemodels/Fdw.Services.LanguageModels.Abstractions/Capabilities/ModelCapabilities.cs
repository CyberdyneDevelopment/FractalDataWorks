using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The TypeCollection of what a configured model can carry. A consumer's own provider package adds options in its own assembly.
/// </summary>
[TypeCollection(typeof(ModelCapabilityBase), typeof(IModelCapability), typeof(ModelCapabilities))]
[ExcludeFromCodeCoverage]
public abstract partial class ModelCapabilities : TypeCollectionBase<ModelCapabilityBase, IModelCapability>
{
}
