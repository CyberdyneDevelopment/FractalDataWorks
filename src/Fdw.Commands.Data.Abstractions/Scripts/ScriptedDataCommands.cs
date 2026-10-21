using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The scripted commands (FDW-1435): text a person wrote, run by a store that speaks it. They are a family of their own, apart
/// from <c>DataCommands</c>, because every connection answers every data command and only a SQL database answers these.
/// </summary>
[TypeCollection(typeof(ScriptedDataCommandBase), typeof(IScriptedDataCommand), typeof(ScriptedDataCommands))]
[ExcludeFromCodeCoverage]
public abstract partial class ScriptedDataCommands : TypeCollectionBase<ScriptedDataCommandBase, IScriptedDataCommand>
{
}
