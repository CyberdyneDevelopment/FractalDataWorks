namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>
/// The outcome of one evaluation of a compiled calculation: no failure yet, or the first failure and where it happened.
/// </summary>
/// <remarks>
/// Why a state object and not exceptions: a division by zero or an empty operand is ordinary bad data, and a month of
/// intervals can hold thousands of them. The compiled code records the first failure here and carries on with a placeholder
/// value; the caller reads <see cref="Failed"/> once at the end and turns the state into a failed result. The first failure
/// wins, so the result names the earliest problem in evaluation order. One state is reused across records by calling
/// <see cref="Reset"/>.
/// </remarks>
public sealed class CalculationRunState
{
    /// <summary>Gets the id of the failure's result code, or zero when nothing has failed.</summary>
    public int Code { get; private set; }

    /// <summary>Gets the index of the node that failed.</summary>
    public int Node { get; private set; }

    /// <summary>Gets the index of the result that was being computed when the failure happened.</summary>
    public int FailedResult { get; private set; }

    /// <summary>Gets the reason the failure gives, or an empty text when the code says it all.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Gets or sets the index of the result being computed now.</summary>
    public int Current { get; set; }

    /// <summary>Gets a value indicating whether anything has failed.</summary>
    public bool Failed => Code != 0;

    /// <summary>Records a failure unless one is already recorded.</summary>
    /// <param name="code">The id of the result code.</param>
    /// <param name="node">The index of the failing node.</param>
    /// <returns>Zero, the placeholder the failed operation yields.</returns>
    public decimal Fail(int code, int node) => FailWith(code, node, string.Empty);

    /// <summary>Records a failure with a reason unless one is already recorded.</summary>
    /// <param name="code">The id of the result code.</param>
    /// <param name="node">The index of the failing node.</param>
    /// <param name="reason">The reason to show.</param>
    /// <returns>Zero, the placeholder the failed operation yields.</returns>
    public decimal FailWith(int code, int node, string reason)
    {
        if (Code == 0)
        {
            Code = code;
            Node = node;
            FailedResult = Current;
            Reason = reason;
        }

        return 0m;
    }

    /// <summary>Records a failure unless one is already recorded.</summary>
    /// <param name="code">The id of the result code.</param>
    /// <param name="node">The index of the failing node.</param>
    /// <returns>False, the placeholder the failed condition yields.</returns>
    public bool FailFlag(int code, int node)
    {
        Fail(code, node);
        return false;
    }

    /// <summary>Records a failure with a reason unless one is already recorded.</summary>
    /// <param name="code">The id of the result code.</param>
    /// <param name="node">The index of the failing node.</param>
    /// <param name="reason">The reason to show.</param>
    /// <returns>False, the placeholder the failed condition yields.</returns>
    public bool FailFlagWith(int code, int node, string reason)
    {
        FailWith(code, node, reason);
        return false;
    }

    /// <summary>Clears the state so it can be used for the next record.</summary>
    public void Reset()
    {
        Code = 0;
        Node = 0;
        FailedResult = 0;
        Reason = string.Empty;
        Current = 0;
    }
}
