using System;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// Reads the fixed arguments the built-in procedures take: a handle, a parameter list, a statement.
/// </summary>
public static class ProcedureArguments
{
    /// <summary>The type a prepared-statement handle is handed back as.</summary>
    public static readonly TdsColumnType HandleType = new(SqlDataTypes.ByName("Int"), 0, 0, 0);

    /// <summary>Reads a text argument, by name or by position.</summary>
    /// <param name="call">The call.</param>
    /// <param name="name">The parameter's name, with its @.</param>
    /// <param name="position">Its position, from 0.</param>
    /// <param name="required">Whether a call without it is refused; otherwise it reads as empty text.</param>
    /// <returns>The text, or the refusal.</returns>
    public static IGenericResult<string> Text(SqlProcedureCall call, string name, int position, bool required)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (!call.TryArgument(name, position, out var argument) || argument.Value is null)
        {
            return required
                ? SqlRefusal.Refuse<string>(SqlServerErrors.BadParameters(call.Procedure, $"{name} was not supplied"), call.Context.Logger)
                : GenericResult<string>.Success(string.Empty);
        }

        return GenericResult<string>.Success(SqlValues.ToText(argument.Value));
    }

    /// <summary>Reads a handle argument, by name or by position.</summary>
    /// <param name="call">The call.</param>
    /// <param name="name">The parameter's name, with its @.</param>
    /// <param name="position">Its position, from 0.</param>
    /// <returns>The handle, or the refusal.</returns>
    public static IGenericResult<int> Handle(SqlProcedureCall call, string name, int position)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (call.TryArgument(name, position, out var argument) && argument.Value is { } value
            && SqlValues.TryNumber(value, out var number) && number is >= int.MinValue and <= int.MaxValue)
        {
            return GenericResult<int>.Success((int)number);
        }

        return SqlRefusal.Refuse<int>(SqlServerErrors.BadParameters(call.Procedure, $"{name} is not a prepared-statement handle"), call.Context.Logger);
    }

    /// <summary>Hands a new handle back through the call's handle argument.</summary>
    /// <param name="call">The call.</param>
    /// <param name="handle">The handle.</param>
    /// <returns>The output, against the argument at position 0.</returns>
    public static SqlProcedureOutput HandleOutput(SqlProcedureCall call, int handle)
    {
        ArgumentNullException.ThrowIfNull(call);
        var name = call.Arguments.Count > 0 ? call.Arguments[0].Name : string.Empty;
        return new SqlProcedureOutput(0, name, handle, HandleType);
    }
}
