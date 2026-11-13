using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>One problem the check before save found in a Graph data set draft (FDW-5290): its code, what is wrong, and where.</summary>
/// <remarks>
/// It has the members of a graph pipeline's problem (<c>path</c> and <c>reason</c>, here <see cref="Message"/>) and adds the code, because a
/// data set's every failure reason has its own coded message. <see cref="Message"/> is the code's message formatted with the facts of this
/// problem, never text typed at the place the problem is found.
/// </remarks>
/// <param name="Code">The result code, for example <c>DATA-21400</c>.</param>
/// <param name="Message">What is wrong.</param>
/// <param name="Path">Where: <c>fieldsOut/Amount</c>, <c>components/Join1</c> or <c>links/3</c> as separate segments.</param>
public sealed record GraphDataSetProblem(string Code, string Message, IReadOnlyList<string> Path);
