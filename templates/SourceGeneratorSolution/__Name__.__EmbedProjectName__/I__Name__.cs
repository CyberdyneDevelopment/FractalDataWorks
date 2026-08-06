using System;

namespace __RootNamespace__.__Name__.Abstractions;

/// <summary>
/// Defines the contract for a __Name__ implementation.
/// </summary>
public interface I__Name__
{
    /// <summary>
    /// Gets the unique name of this __Name__.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the __Name__ operation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    System.Threading.Tasks.Task ExecuteAsync();
}
