using System.Collections.Generic;
using Fdw.Processors;
using Fdw.Results;

namespace Fdw.Services.Connections;

/// <summary>
/// Base class for connection processors.
/// Provides the "Connection" category automatically.
/// </summary>
/// <typeparam name="TCommand">The command type being processed.</typeparam>
/// <typeparam name="TContext">The processing context.</typeparam>
/// <typeparam name="TBase">The concrete base type (CRTP self-reference).</typeparam>
/// <remarks>
/// <para>
/// This base class extends <see cref="ProcessorBase{TCommand, TContext, TBase}"/>
/// with connection-specific defaults. The category is automatically set to "Connection".
/// </para>
/// <para>
/// Domain-specific connection processors (e.g., MsSql, PostgreSql) should create
/// their own base class that inherits from this, adding domain-specific validation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public abstract class MsSqlAuthProcessorBase
///     : ConnectionProcessorBase&lt;StringBuilder, MsSqlContext, MsSqlAuthProcessorBase&gt;
/// {
///     // Add MsSql-specific validation
/// }
/// </code>
/// </example>
public abstract class ConnectionProcessorBase<TCommand, TContext, TBase>
    : ProcessorBase<TCommand, TContext, TBase>,
      IConnectionProcessor<TCommand, TContext>
    where TBase : ConnectionProcessorBase<TCommand, TContext, TBase>
{
    /// <summary>
    /// Initializes a new instance for the Empty/NotFound sentinel.
    /// </summary>
    protected ConnectionProcessorBase()
    {
    }

    /// <summary>
    /// Initializes a new instance with connection-specific metadata.
    /// </summary>
    /// <param name="name">The processor identifier (e.g., "SqlAuth", "WindowsAuth").</param>
    /// <param name="displayName">Human-readable name for UI display.</param>
    /// <param name="description">Description of this authentication/processing method.</param>
    /// <param name="requiredProperties">Required context property names for validation.</param>
    /// <remarks>
    /// The category is automatically set to "Connection" for all connection processors.
    /// </remarks>
    protected ConnectionProcessorBase(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> requiredProperties)
        : base(name, displayName, description, requiredProperties, "Connection")
    {
    }
}
