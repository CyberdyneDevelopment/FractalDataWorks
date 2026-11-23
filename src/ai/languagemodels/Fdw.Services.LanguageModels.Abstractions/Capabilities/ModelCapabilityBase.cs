using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="ModelCapabilities"/>: what a configured model can carry.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ModelCapabilityBase : TypeOptionBase<ModelCapabilityBase>, IModelCapability
{
    /// <summary>Initializes a new instance of the <see cref="ModelCapabilityBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    protected ModelCapabilityBase(string name) : base(name)
    {
    }
}
