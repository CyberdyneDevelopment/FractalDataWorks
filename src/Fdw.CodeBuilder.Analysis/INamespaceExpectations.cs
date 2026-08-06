using System;

namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for namespace expectations.
/// </summary>
public interface INamespaceExpectations
{
    /// <summary>
    /// Expects the namespace to contain a class.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <param name="expectations">Additional expectations for the class.</param>
    /// <returns>The expectations instance for chaining.</returns>
    INamespaceExpectations HasClass(string className, Action<IClassExpectations>? expectations = null);

    /// <summary>
    /// Expects the namespace to contain an interface.
    /// </summary>
    /// <param name="interfaceName">The interface name.</param>
    /// <param name="expectations">Additional expectations for the interface.</param>
    /// <returns>The expectations instance for chaining.</returns>
    INamespaceExpectations HasInterface(string interfaceName, Action<IInterfaceExpectations>? expectations = null);
}
