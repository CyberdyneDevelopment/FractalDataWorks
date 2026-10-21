using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Says whether a step may set a name (FDW-1549): it has to be declared in scope, and a parameter the pipeline declared read-only is
/// not set by any step. One place, so every step that keeps a result in a variable refuses the same way.
/// </summary>
public static class FlowVariableTarget
{
    /// <summary>Judges a name a step would set.</summary>
    /// <param name="variables">The scope the step runs in.</param>
    /// <param name="step">The step's name, for the message.</param>
    /// <param name="variable">The name the step would set.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success when the step may set the name; a failure naming why not otherwise.</returns>
    public static IGenericResult Check(FlowVariables variables, string step, string variable, ILogger logger)
    {
        if (!variables.IsDeclared(variable))
        {
            return GenericResult.Failure(FlowLog.VariableNotDeclared(logger, step, variable));
        }

        return variables.IsReadOnly(variable)
            ? GenericResult.Failure(FlowLog.VariableReadOnly(logger, step, variable))
            : GenericResult.Success();
    }
}
