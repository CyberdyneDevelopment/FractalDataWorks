using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to find all TypeCollection definitions in the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindTypeCollections")]
public sealed class FindTypeCollectionsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindTypeCollectionsCommand"/> class.
    /// </summary>
    public FindTypeCollectionsCommand()
        : base("FindTypeCollections", RoslynCommandCategories.Conventions, "Find every type marked as a TypeCollection (the FDW source-generated registry pattern). Use to enumerate the families of options the framework discovers via module initializer. Returns TypeCollectionInfo entries with name, item type, and file/line.")
    {
    }
}
