using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Why a pipeline run that has been accepted is not running yet: a limit, a table another run writes, or a run ahead of it (FDW-3097).
/// </summary>
public interface IRunWaitReason : ITypeOption<int, IRunWaitReason>
{
}
