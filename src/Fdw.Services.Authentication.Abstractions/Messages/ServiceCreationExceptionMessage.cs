using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that an exception occurred during service creation.
/// </summary>
[Message("ServiceCreationException")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class ServiceCreationExceptionMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceCreationExceptionMessage"/> class.
    /// </summary>
    /// <param name="exceptionMessage">The exception message.</param>
    public ServiceCreationExceptionMessage(string exceptionMessage)
        : base(1008, "ServiceCreationException", MessageSeverity.Error,
               exceptionMessage, "AUTH_SERVICE_CREATION_FAILED")
    { }
}
