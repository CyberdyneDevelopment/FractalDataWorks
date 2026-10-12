using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// TypeCollection for all data command types.
/// Source generator will create static properties for each command with [TypeOption] attribute.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides compile-time discovery of all data command types.
/// Each command type (Query, Insert, Update, Delete, Upsert, BulkInsert) will have a static property.
/// </para>
/// <para>
/// Example generated properties:
/// <list type="bullet">
/// <item>DataCommands.Query - QueryCommand type</item>
/// <item>DataCommands.Insert - InsertCommand type</item>
/// <item>DataCommands.Update - UpdateCommand type</item>
/// </list>
/// </para>
/// <para>
/// The source generator also creates:
/// <list type="bullet">
/// <item>All() - Returns all command types</item>
/// <item>ByName(string name) - Finds command by name</item>
/// <item>ById(int id) - Finds command by id</item>
/// </list>
/// </para>
/// </remarks>
[TypeCollection(typeof(DataCommandBase), typeof(IDataCommand), typeof(DataCommands))]
[ExcludeFromCodeCoverage]
public abstract partial class DataCommands : TypeCollectionBase<DataCommandBase, IDataCommand>
{
    // Source generator will create:
    // - Static constructor
    // - Static properties for each [TypeOption] command
    // - All() method
    // - ByName() method
    // - ById() method
}
