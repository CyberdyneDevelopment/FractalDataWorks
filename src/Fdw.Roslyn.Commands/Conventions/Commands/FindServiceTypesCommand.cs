using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to find all ServiceType implementations in the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindServiceTypes")]
public sealed class FindServiceTypesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindServiceTypesCommand"/> class.
    /// </summary>
    public FindServiceTypesCommand()
        : base("FindServiceTypes", RoslynCommandCategories.Conventions, "Find every implementation of the FDW ServiceType pattern (decorated with [ServiceType] or deriving from a ServiceType base). Use as the discovery step before auditing service-type registrations or running family-drift analysis on a ServiceType collection. Returns ServiceTypeInfo entries with name, namespace, and file/line.")
    {
    }
}
