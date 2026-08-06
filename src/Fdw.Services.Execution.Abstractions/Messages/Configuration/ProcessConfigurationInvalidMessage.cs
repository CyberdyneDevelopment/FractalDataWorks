using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Configuration;

/// <summary>
/// CurrentMessage indicating that process configuration is invalid.
/// </summary>
[Message("ProcessConfigurationInvalid")]
public sealed class ProcessConfigurationInvalidMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationInvalidMessage"/> class.
    /// </summary>
    public ProcessConfigurationInvalidMessage()
        : base(2001, "ProcessConfigurationInvalid", MessageSeverity.Error,
               "Process configuration is invalid",
               "EXEC_CONFIG_INVALID",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }

    /// <summary>
    /// Initializes a new instance with process type.
    /// </summary>
    /// <param name="processType">The type of process with invalid configuration.</param>
    public ProcessConfigurationInvalidMessage(string processType)
        : base(2001, "ProcessConfigurationInvalid", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Configuration is invalid for process type: {0}", processType),
               "EXEC_CONFIG_INVALID",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }

    /// <summary>
    /// Initializes a new instance with process type and validation details.
    /// </summary>
    /// <param name="processType">The type of process with invalid configuration.</param>
    /// <param name="validationDetails">Details about what configuration is invalid.</param>
    public ProcessConfigurationInvalidMessage(string processType, string validationDetails)
        : base(2001, "ProcessConfigurationInvalid", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Configuration is invalid for process type: {0}, details: {1}",
                           processType, validationDetails),
               "EXEC_CONFIG_INVALID",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }

    /// <summary>
    /// Initializes a new instance with process ID, process type, and validation details.
    /// </summary>
    /// <param name="processId">The ID of the process with invalid configuration.</param>
    /// <param name="processType">The type of process with invalid configuration.</param>
    /// <param name="validationDetails">Details about what configuration is invalid.</param>
    public ProcessConfigurationInvalidMessage(string processId, string processType, string validationDetails)
        : base(2001, "ProcessConfigurationInvalid", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Configuration is invalid for process: {0} (type: {1}), details: {2}",
                           processId, processType, validationDetails),
               "EXEC_CONFIG_INVALID",
               "Configuration",
               "https://docs.Fdw.io/execution/configuration")
    { }
}