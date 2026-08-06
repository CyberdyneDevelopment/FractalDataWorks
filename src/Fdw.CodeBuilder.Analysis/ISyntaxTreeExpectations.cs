using System;

namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for syntax tree expectations.
/// </summary>
public interface ISyntaxTreeExpectations
{
    /// <summary>
    /// Expects the syntax tree to contain a namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <param name="expectations">Additional expectations for the namespace.</param>
    /// <returns>The expectations instance for chaining.</returns>
    ISyntaxTreeExpectations HasNamespace(string namespaceName, Action<INamespaceExpectations>? expectations = null);

    /// <summary>
    /// Expects the syntax tree to contain a class.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <param name="expectations">Additional expectations for the class.</param>
    /// <returns>The expectations instance for chaining.</returns>
    ISyntaxTreeExpectations HasClass(string className, Action<IClassExpectations>? expectations = null);

    /// <summary>
    /// Expects the syntax tree to contain an interface.
    /// </summary>
    /// <param name="interfaceName">The interface name.</param>
    /// <param name="expectations">Additional expectations for the interface.</param>
    /// <returns>The expectations instance for chaining.</returns>
    ISyntaxTreeExpectations HasInterface(string interfaceName, Action<IInterfaceExpectations>? expectations = null);

    /// <summary>
    /// Expects the syntax tree to contain an enum.
    /// </summary>
    /// <param name="enumName">The enum name.</param>
    /// <param name="expectations">Additional expectations for the enum.</param>
    /// <returns>The expectations instance for chaining.</returns>
    ISyntaxTreeExpectations HasEnum(string enumName, Action<IEnumExpectations>? expectations = null);

    /// <summary>
    /// Expects the syntax tree to contain a record.
    /// </summary>
    /// <param name="recordName">The record name.</param>
    /// <param name="expectations">Additional expectations for the record.</param>
    /// <returns>The expectations instance for chaining.</returns>
    ISyntaxTreeExpectations HasRecord(string recordName, Action<IRecordExpectations>? expectations = null);

    /// <summary>
    /// Asserts that the syntax tree compiles without errors.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    ISyntaxTreeExpectations Compiles();
}
