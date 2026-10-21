namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// How a guide's named exemplar was found in the loaded solution.
/// </summary>
public enum GuideExemplarResolution
{
    /// <summary>The exemplar's file is in the solution and declares the type; location and summary are live.</summary>
    Resolved,
    /// <summary>The exemplar's file is not part of the loaded solution, so only the guide's stamped citation is known.</summary>
    FileNotInSolution,
    /// <summary>The file is in the solution but no longer declares the type; the guide has drifted.</summary>
    TypeNotInFile,
}
