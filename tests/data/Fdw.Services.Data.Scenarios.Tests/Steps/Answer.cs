using System.Text.Json;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>The last answer a scenario received: the status, the whole body as text and the body read as JSON when it is JSON.</summary>
internal sealed class Answer
{
    internal Answer(int status, string body)
    {
        Status = status;
        Body = body;
    }

    internal int Status { get; }

    internal string Body { get; }

    internal JsonDocument Json() => JsonDocument.Parse(Body);
}
