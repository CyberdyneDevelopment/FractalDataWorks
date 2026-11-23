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

/// <summary>The path ends before it names a kind.</summary>
[TypeOption(typeof(DataOptionPathProblems), "DataOptionPathEndsBeforeKind")]
[ExcludeFromCodeCoverage]
public sealed class EndsBeforeKindDataOptionPathProblem : DataOptionPathProblemBase
{
    /// <summary>Initializes a new instance of the <see cref="EndsBeforeKindDataOptionPathProblem"/> class.</summary>
    public EndsBeforeKindDataOptionPathProblem()
        : base("DataOptionPathEndsBeforeKind")
    {
    }

    /// <inheritdoc />
    public override IGenericMessage Message(ILogger logger, IResultDetails details, IReadOnlyList<string> path, string text)
        => DataOptionRulesLog.PathIncomplete(logger, text, details.GetValue<string>("Name")!, details.GetValue<string>("Known")!);
}
