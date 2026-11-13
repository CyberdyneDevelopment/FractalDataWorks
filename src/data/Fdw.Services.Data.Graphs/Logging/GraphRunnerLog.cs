using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Graphs.Logging;

/// <summary>Messages of the Graph data set runner's registration (FDW-5291).</summary>
[MessageLoggingTypeCode("GRAPHRUN")]
public static partial class GraphRunnerLog
{
    /// <summary>A stated limit of the runner is not a whole number.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="section">The configuration section that states the limits.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 59801,
        Level = LogLevel.Error,
        Message = "A limit stated under '{section}' for reading Graph data sets is not a whole number, so the runner is not registered")]
    public static partial IGenericMessage LimitNotANumber(ILogger logger, string section);
}
