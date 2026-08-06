using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Base class for result severity implementations using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ResultSeverityBase : TypeOptionBase<int, ResultSeverityBase>, IResultSeverity
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ResultSeverityBase()
        : base(-1, "NotFound")
    {
        IsSuccess = false;
        IsFailure = true;
        LogLevelValue = 4; // Error
        ShouldLog = true;
        ColorHint = "gray";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultSeverityBase"/> class.
    /// </summary>
    protected ResultSeverityBase(
        int id,
        string name,
        bool isSuccess,
        int logLevelValue,
        bool shouldLog,
        string colorHint)
        : base(id, name)
    {
        IsSuccess = isSuccess;
        IsFailure = !isSuccess;
        LogLevelValue = logLevelValue;
        ShouldLog = shouldLog;
        ColorHint = colorHint;
    }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure { get; }

    /// <inheritdoc />
    public int LogLevelValue { get; }

    /// <inheritdoc />
    public bool ShouldLog { get; }

    /// <inheritdoc />
    public string ColorHint { get; }
}
