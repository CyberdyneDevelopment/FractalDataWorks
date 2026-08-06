using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that service type casting failed.
/// The source generator will create FactoryMessages.ServiceTypeCastFailed(sourceType, targetType) method.
/// </summary>
[Message("ServiceTypeCastFailed")]
public sealed class ServiceTypeCastFailedMessage : FactoryMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTypeCastFailedMessage"/> class with default message template.
    /// </summary>
    public ServiceTypeCastFailedMessage()
        : base(2003, "ServiceTypeCastFailed", MessageSeverity.Error,
               "Service type cast failed from {0} to {1}", "SERVICE_TYPE_CAST_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with the source and target types that failed to cast.
    /// </summary>
    /// <param name="sourceType">The source type being cast from.</param>
    /// <param name="targetType">The target type being cast to.</param>
    public ServiceTypeCastFailedMessage(string sourceType, string targetType)
        : base(2003, "ServiceTypeCastFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Service type cast failed from {0} to {1}", sourceType, targetType),
               "SERVICE_TYPE_CAST_FAILED")
    { }

}