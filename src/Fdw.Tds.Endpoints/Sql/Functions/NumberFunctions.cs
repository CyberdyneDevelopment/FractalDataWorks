using System;
using System.Globalization;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Logging;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// The number functions' shared rule: the result keeps the argument's own type.
/// </summary>
public static class NumberFunctions
{
    /// <summary>Applies an operation in the argument's own type: whole, exact or floating.</summary>
    /// <param name="call">The call, for the refusal.</param>
    /// <param name="value">The argument; not NULL.</param>
    /// <param name="whole">The operation on a whole number.</param>
    /// <param name="exact">The operation on a decimal.</param>
    /// <param name="floating">The operation on a float.</param>
    /// <returns>The result, or error 245 for a value that is not a number and 8115 for an overflow.</returns>
    public static IGenericResult<object?> Map(
        SqlFunctionCall call, object value, Func<long, long> whole, Func<decimal, decimal> exact, Func<double, double> floating)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(whole);
        ArgumentNullException.ThrowIfNull(exact);
        ArgumentNullException.ThrowIfNull(floating);
        try
        {
            return value switch
            {
                int i => SqlFunctionCall.Ok(checked((int)whole(i))),
                long l => SqlFunctionCall.Ok(whole(l)),
                short or byte => SqlFunctionCall.Ok(checked((int)whole(Convert.ToInt64(value, CultureInfo.InvariantCulture)))),
                double d => SqlFunctionCall.Ok(floating(d)),
                float f => SqlFunctionCall.Ok((float)floating(f)),
                _ => SqlValues.TryNumber(value, out var number) ? SqlFunctionCall.Ok(exact(number)) : call.NotConvertible("numeric"),
            };
        }
        catch (OverflowException ex)
        {
            TdsEndpointLog.ArithmeticOverflowed(call.Context.Logger, ex, call.Line);
            return call.Refuse(new SqlServerMessage(8115, 2, 16, "Arithmetic overflow error.", call.Line));
        }
    }

    /// <summary>ROUND: half away from zero to the given digits, or cut there; negative digits round left of the point.</summary>
    /// <param name="call">The call, for the refusal.</param>
    /// <param name="value">The number; not NULL.</param>
    /// <param name="digits">The digits to keep after the point; negative counts before it.</param>
    /// <param name="truncate">Whether to cut rather than round.</param>
    /// <returns>The result in the number's own type, or the refusal.</returns>
    public static IGenericResult<object?> Round(SqlFunctionCall call, object value, int digits, bool truncate)
    {
        var step = (decimal)Math.Pow(10, Math.Min(Math.Abs(digits), 28));
        var mode = truncate ? MidpointRounding.ToZero : MidpointRounding.AwayFromZero;
        return Map(call, value,
            n => digits >= 0 ? n : (long)(Math.Round(n / step, 0, mode) * step),
            n => digits >= 0 ? Math.Round(n, Math.Min(digits, 28), mode) : Math.Round(n / step, 0, mode) * step,
            n => digits >= 0 ? Math.Round(n, Math.Min(digits, 15), mode) : Math.Round(n / (double)step, 0, mode) * (double)step);
    }
}
