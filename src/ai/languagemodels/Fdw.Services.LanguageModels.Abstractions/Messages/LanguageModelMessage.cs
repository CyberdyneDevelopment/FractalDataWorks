using System;
using System.Collections.Generic;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>One message of a conversation: a role and the parts it is made of.</summary>
public sealed class LanguageModelMessage
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelMessage"/> class.</summary>
    /// <param name="role">Who the message is from.</param>
    /// <param name="parts">The parts of the message.</param>
    public LanguageModelMessage(IMessageRole role, IReadOnlyList<LanguageModelMessagePart> parts)
    {
        Role = role;
        Parts = parts;
    }

    /// <summary>Gets who the message is from.</summary>
    public IMessageRole Role { get; }

    /// <summary>Gets the parts of the message.</summary>
    public IReadOnlyList<LanguageModelMessagePart> Parts { get; }
}
