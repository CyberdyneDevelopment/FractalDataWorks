using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Options;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>The path goes on after it named a kind.</summary>
[TypeOption(typeof(DataOptionPathProblems), "DataOptionPathContinuesPastKind")]
[ExcludeFromCodeCoverage]
public sealed class ContinuesPastKindDataOptionPathProblem : DataOptionPathProblemBase
{
    /// <summary>Initializes a new instance of the <see cref="ContinuesPastKindDataOptionPathProblem"/> class.</summary>
    public ContinuesPastKindDataOptionPathProblem()
        : base("DataOptionPathContinuesPastKind")
    {
    }

    /// <inheritdoc />
    public override IGenericMessage Message(ILogger logger, IResultDetails details, IReadOnlyList<string> path, string text)
    {
        var extra = details.GetValue<string>("Extra")!;
        return DataOptionRulesLog.PathSegmentNotFound(logger, path.ToList().IndexOf(extra) + 1, extra, string.Empty);
    }
}
