namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A script that is a command (FDW-1435): it changes data or structure and answers how many rows it affected.
/// </summary>
public interface IScriptRunCommand : IScriptedDataCommand<int>
{
}
