namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks a command that advances the workspace baseline to the current solution.
/// </summary>
/// <remarks>
/// Setting a baseline moves the comparison point; it does NOT discard the change ledger, which records
/// what has been done. Conflating the two silently destroyed the record the migration guide is built
/// from — see <see cref="ILedgerClearingCommand"/>, which is the only thing that may.
/// </remarks>
public interface IBaselineSettingCommand
{
}
