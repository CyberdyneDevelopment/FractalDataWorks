using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Messages.Attributes;

namespace Fdw.Messages;

/// <summary>
/// Standard error message for not implemented features.
/// </summary>
[Message("StandardMessages")]
[MessageOption(typeof(MessageCollectionBase<GenericMessage>))]
[ExcludeFromCodeCoverage]
public sealed class NotImplementedMessage : MessageTemplate<MessageSeverity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotImplementedMessage"/> class.
    /// </summary>
    /// <param name="featureDescription">Description of the feature that is not implemented.</param>
    public NotImplementedMessage(string featureDescription)
        : base(
            id: 1005,
            name: "NotImplemented",
            severity: MessageSeverity.Error,
            message: featureDescription,
            code: "NOT_IMPLEMENTED",
            source: "Implementation")
    {
        FeatureDescription = featureDescription;
    }

    /// <summary>
    /// Gets the description of the feature that is not implemented.
    /// </summary>
    public string FeatureDescription { get; }
}