using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>A layer registered under the DataConfigurationRules root (FDW-6340).</summary>
public interface IRuleLayerOption : ITypeOption<int, RuleLayerBase>, IRuleLayer
{
    /// <summary>Gets the name a path states at this level. Stated here once, because the type option and the rule layer both carry it and the collection generator reads it on this interface.</summary>
    new string Name { get; }
}
