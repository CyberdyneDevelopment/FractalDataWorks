using System;

namespace __RootNamespace__.__Name__;

/// <summary>
/// Base class for __Name__ implementations.
/// </summary>
/// <typeparam name="TConfiguration">The configuration type for this __Name__.</typeparam>
public abstract class __Name__Base<TConfiguration>
    where TConfiguration : class
{
    /// <summary>
    /// Gets the unique name of this __Name__.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Validates the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public virtual bool ValidateConfiguration(TConfiguration configuration)
    {
        return configuration != null;
    }
}
