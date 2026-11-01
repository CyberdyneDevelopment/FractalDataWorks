using System;
using System.Collections.Generic;
using Fdw.Commands.Data;

namespace Fdw.Commands.Data.Extensions;

/// <summary>
/// Fluent builder for insert commands.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for creating insert commands:
/// <code>
/// // Single insert with full path specification
/// var cmd = Insert.Into&lt;Customer&gt;("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .Value(customer);
///
/// // Many rows (the connection takes its bulk path above its configured MaxSerialInsert)
/// var cmd = Insert.IntoMany&lt;Customer&gt;("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .Values(customers);
/// </code>
/// </para>
/// </remarks>
public static class Insert
{
    /// <summary>
    /// Starts building an insert command for a single entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <returns>A builder for single-entity inserts.</returns>
    public static InsertSingleBuilder<T> Into<T>(string containerName)
    {
        return new InsertSingleBuilder<T>(containerName);
    }

    /// <summary>
    /// Starts building an insert command for multiple entities.
    /// The connection runs the rows in batched multi-row INSERT statements, or takes its bulk path above its configured MaxSerialInsert.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <returns>A builder for batch inserts.</returns>
    public static InsertBatchBuilder<T> IntoMany<T>(string containerName)
    {
        return new InsertBatchBuilder<T>(containerName);
    }
}