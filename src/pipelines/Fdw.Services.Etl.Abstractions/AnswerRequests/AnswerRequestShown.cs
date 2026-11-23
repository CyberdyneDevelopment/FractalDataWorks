namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The names of the things a person is shown with an answer request (FDW-6142): the rows <see cref="AnswerRequestAsk.Shown"/> and <see cref="AnswerRequest.Shown"/> carry.</summary>
public static class AnswerRequestShown
{
    /// <summary>The title of the request.</summary>
    public const string Title = "title";

    /// <summary>The body of the request.</summary>
    public const string Body = "body";

    /// <summary>One option of a choice; a choice shows one row of this name for each option, in order.</summary>
    public const string Option = "option";
}
