using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Http.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that HTTP configuration is invalid.
/// </summary>
[Message("HttpConfigurationInvalid")]
[MessageOption(typeof(HttpMessageCollectionBase))]
public sealed class HttpConfigurationInvalidMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConfigurationInvalidMessage"/> class.
    /// </summary>
    public HttpConfigurationInvalidMessage()
        : base(4301, "HttpConfigurationInvalid", MessageSeverity.Error,
               "HTTP configuration is invalid",
               "HTTP_CONFIG_INVALID")
    { }

    /// <summary>
    /// Initializes a new instance with configuration field.
    /// </summary>
    /// <param name="fieldName">The configuration field that is invalid.</param>
    public HttpConfigurationInvalidMessage(string fieldName)
        : base(4301, "HttpConfigurationInvalid", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP configuration field '{0}' is invalid", fieldName),
               "HTTP_CONFIG_INVALID")
    { }

    /// <summary>
    /// Initializes a new instance with field and validation error.
    /// </summary>
    /// <param name="fieldName">The configuration field that is invalid.</param>
    /// <param name="validationError">The validation error details.</param>
    public HttpConfigurationInvalidMessage(string fieldName, string validationError)
        : base(4301, "HttpConfigurationInvalid", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP configuration field '{0}' is invalid: {1}", fieldName, validationError),
               "HTTP_CONFIG_INVALID")
    { }

    /// <summary>
    /// Initializes a new instance with field, validation error, and expected format.
    /// </summary>
    /// <param name="fieldName">The configuration field that is invalid.</param>
    /// <param name="validationError">The validation error details.</param>
    /// <param name="expectedFormat">The expected format for the field.</param>
    public HttpConfigurationInvalidMessage(string fieldName, string validationError, string expectedFormat)
        : base(4301, "HttpConfigurationInvalid", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP configuration field '{0}' is invalid: {1}, expected format: {2}", fieldName, validationError, expectedFormat),
               "HTTP_CONFIG_INVALID")
    { }
}