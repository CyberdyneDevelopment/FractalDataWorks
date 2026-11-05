using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Validation.Configuration;

/// <summary>
/// The configuration classes a write checks, each with its validator or the reason it needs none (FDW-3541).
/// </summary>
/// <remarks>
/// Options are declared in the assembly that owns the configuration class and reach a host through its generated module initializer, so
/// this collection holds what the host references. A lookup that finds nothing returns <c>NotFound</c> and the write is refused with a
/// coded message; nothing is validated by silence.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ConfigurationValidatorBase), typeof(IConfigurationValidator), typeof(ConfigurationValidators))]
public abstract partial class ConfigurationValidators : TypeCollectionBase<ConfigurationValidatorBase, IConfigurationValidator>
{
}
