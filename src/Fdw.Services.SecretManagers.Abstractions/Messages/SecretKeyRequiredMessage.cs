using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that SecretKey is required for an operation.
/// </summary>
[Message("SecretKeyRequired")]
[MessageOption(typeof(SecretManagerMessageCollectionBase))]
public sealed class SecretKeyRequiredMessage : SecretManagerMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretKeyRequiredMessage"/> class.
    /// </summary>
    /// <param name="operation">The operation that requires the secret key.</param>
    public SecretKeyRequiredMessage(string operation)
        : base(1002, "SecretKeyRequired", MessageSeverity.Error,
               $"SecretKey is required for {operation} operation.", "SM_KEY_REQUIRED")
    { }
}
