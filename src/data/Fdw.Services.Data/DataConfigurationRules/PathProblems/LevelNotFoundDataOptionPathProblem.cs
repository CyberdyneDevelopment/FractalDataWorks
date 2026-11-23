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

/// <summary>The path names a level the options do not have.</summary>
[TypeOption(typeof(DataOptionPathProblems), "DataOptionPathLevelNotFound")]
[ExcludeFromCodeCoverage]
public sealed class LevelNotFoundDataOptionPathProblem : DataOptionPathProblemBase
{
    /// <summary>Initializes a new instance of the <see cref="LevelNotFoundDataOptionPathProblem"/> class.</summary>
    public LevelNotFoundDataOptionPathProblem()
        : base("DataOptionPathLevelNotFound")
    {
    }

    /// <inheritdoc />
    public override IGenericMessage Message(ILogger logger, IResultDetails details, IReadOnlyList<string> path, string text)
    {
        var level = details.GetValue<int>("Level");
        return level == 1
            ? DataOptionRulesLog.PathRootNotFound(logger, details.GetValue<string>("Name")!, nameof(Fdw.Data.Options.DataConfigurationRules))
            : DataOptionRulesLog.PathSegmentNotFound(logger, level, details.GetValue<string>("Name")!, details.GetValue<string>("Known")!);
    }
}
