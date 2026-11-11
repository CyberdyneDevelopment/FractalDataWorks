using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Logging;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Base class for result code implementations using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ResultCodeBase : TypeOptionBase<int, ResultCodeBase>, IResultCode
{
    private static readonly Regex PlaceholderRegex = new(@"\{(?<key>\w+)\}", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ResultCodeBase()
        : base(0, "NotFound")
    {
        Code = "UNKNOWN";
        EventId = 0;
        Severity = null!;
        Domain = "Unknown";
        MessageTemplate = "An unknown error occurred.";
        IsRetryable = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultCodeBase"/> class.
    /// </summary>
    protected ResultCodeBase(
        [TypeOptionId] int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string domain,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name)
    {
        Code = code;
        EventId = eventId;
        Severity = severity;
        Domain = domain;
        MessageTemplate = messageTemplate;
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> — the catalog scheme
    /// where the number is the whole identity: Id == EventId == number and Code == "{prefix}-{number}".
    /// The handling category is a function of the number (number / 10000), resolved via
    /// ResultCategories where it is needed; the code itself only carries the number.
    /// </summary>
    protected ResultCodeBase(
        [TypeOptionId] int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        string prefix,
        bool isRetryable = false)
        : base(number, name)
    {
        Code = $"{prefix}-{number}";
        EventId = number;
        Severity = severity;
        Domain = prefix;
        MessageTemplate = messageTemplate;
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// Gets the string code identifier — <c>{prefix}-{number}</c>, e.g. <c>MESSAGING-91000</c>.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the event ID for logging (matches MessageLogging EventId pattern).
    /// </summary>
    public int EventId { get; }

    /// <inheritdoc />
    public IResultSeverity Severity { get; }

    /// <inheritdoc />
    public string Domain { get; }

    /// <inheritdoc />
    public string MessageTemplate { get; }

    /// <inheritdoc />
    public bool IsRetryable { get; }

    /// <inheritdoc />
    public virtual string FormatMessage(IResultDetails? details = null)
    {
        if (details == null || details.Data.Count == 0)
        {
            return MessageTemplate;
        }

        var result = new StringBuilder(MessageTemplate);

        foreach (Match match in PlaceholderRegex.Matches(MessageTemplate))
        {
            var key = match.Groups["key"].Value;
            if (details.Data.TryGetValue(key, out var value))
            {
                result.Replace(match.Value, value?.ToString() ?? string.Empty);
            }
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public virtual string? FormatClientMessage(IResultDetails? details = null) => null;

    /// <summary>
    /// Gets the LogLevel corresponding to this result code's severity.
    /// </summary>
    /// <remarks>
    /// Why the severity's own number and not a switch on its name: every severity states the log level it stands for
    /// (<see cref="IResultSeverity.LogLevelValue"/>, the numbers of <see cref="Microsoft.Extensions.Logging.LogLevel"/>), so a name
    /// that the switch did not list (Trace, Debug, or a severity a downstream assembly adds) was read as Error. Only the NotFound
    /// sentinel has no severity, and it is an error by definition (FDW-3463).
    /// </remarks>
    public LogLevel LogLevel => Severity is null ? LogLevel.Error : (LogLevel)Severity.LogLevelValue;

    /// <summary>
    /// Logs this result code with the provided details to the logger.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to include in the message.</param>
    public void Log(ILogger logger, IResultDetails? details = null)
    {
        if (logger == null)
        {
            return;
        }

        var message = FormatMessage(details);
#pragma warning disable CA2254 // Template is dynamically formatted from the result code
#pragma warning disable CA1848 // LoggerMessage delegates cannot carry a runtime EventId or level
        logger.Log(LogLevel, new EventId(EventId, Code), message);
#pragma warning restore CA1848
#pragma warning restore CA2254
    }

    /// <summary>
    /// Logs this result code with the provided details and returns self for fluent chaining.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to include in the message.</param>
    /// <returns>This result code instance.</returns>
    public IResultCode LogAndReturn(ILogger logger, IResultDetails? details = null)
    {
        Log(logger, details);
        return this;
    }
}
