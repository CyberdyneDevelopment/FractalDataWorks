using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Options;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>The base of every option of <see cref="DataOptionPathProblems"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class DataOptionPathProblemBase : TypeOptionBase<DataOptionPathProblemBase>, IDataOptionPathProblem
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionPathProblemBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected DataOptionPathProblemBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericMessage Message(ILogger logger, IResultDetails details, IReadOnlyList<string> path, string text);
}
