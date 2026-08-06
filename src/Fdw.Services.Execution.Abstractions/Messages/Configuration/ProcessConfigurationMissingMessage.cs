using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Configuration;

/// <summary>
/// CurrentMessage indicating that required process configuration is missing.
/// </summary>
[Message("ProcessConfigurationMissing")]
public sealed class ProcessConfigurationMissingMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationMissingMessage"/> class.
    /// </summary>
    public ProcessConfigurationMissingMessage()
        : base(2002, "ProcessConfigurationMissing", MessageSeverity.Error,
               "Required process configuration is missing",
               "EXEC_CONFIG_MISSING",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }

    /// <summary>
    /// Initializes a new instance with process type.
    /// </summary>
    /// <param name="processType">The type of process missing configuration.</param>
    public ProcessConfigurationMissingMessage(string processType)
        : base(2002, "ProcessConfigurationMissing", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Required configuration is missing for process type: {0}", processType),
               "EXEC_CONFIG_MISSING",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }

    /// <summary>
    /// Initializes a new instance with process type and missing configuration details.
    /// </summary>
    /// <param name="processType">The type of process missing configuration.</param>
    /// <param name="missingConfigDetails">Details about what configuration is missing.</param>
    public ProcessConfigurationMissingMessage(string processType, string missingConfigDetails)
        : base(2002, "ProcessConfigurationMissing", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Required configuration is missing for process type: {0}, missing: {1}",
                           processType, missingConfigDetails),
               "EXEC_CONFIG_MISSING",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }

    /// <summary>
    /// Initializes a new instance with process ID, process type, and missing configuration details.
    /// </summary>
    /// <param name="processId">The ID of the process missing configuration.</param>
    /// <param name="processType">The type of process missing configuration.</param>
    /// <param name="missingConfigDetails">Details about what configuration is missing.</param>
    public ProcessConfigurationMissingMessage(string processId, string processType, string missingConfigDetails)
        : base(2002, "ProcessConfigurationMissing", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Required configuration is missing for process: {0} (type: {1}), missing: {2}",
                           processId, processType, missingConfigDetails),
               "EXEC_CONFIG_MISSING",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }
}