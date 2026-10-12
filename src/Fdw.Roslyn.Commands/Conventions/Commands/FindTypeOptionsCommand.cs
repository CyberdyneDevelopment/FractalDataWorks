using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to find all TypeOption implementations for a given TypeCollection.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindTypeOptions")]
public sealed class FindTypeOptionsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindTypeOptionsCommand"/> class.
    /// </summary>
    public FindTypeOptionsCommand()
        : base("FindTypeOptions", RoslynCommandCategories.Conventions, "Find every FDW TypeOption implementation, optionally filtered to those belonging to CollectionName. Use to enumerate the concrete options of a TypeCollection family (e.g. all ConnectionTypeOption derivatives). Returns TypeOptionInfo entries with name, ID, collection, and file/line.")
    {
    }
    /// <summary>
    /// Gets or sets the optional collection name filter.
    /// </summary>
    [System.ComponentModel.Description("Optional TypeCollection name to filter results to options of a specific collection (e.g. 'ConnectionTypes'). Null/empty returns all TypeOptions.")]
    public string? CollectionName { get; init; }
}
