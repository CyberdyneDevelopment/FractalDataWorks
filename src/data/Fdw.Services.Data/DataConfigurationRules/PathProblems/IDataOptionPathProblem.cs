using System;
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

/// <summary>One way the option layer refuses a rule's path, named by the code it fails with.</summary>
public interface IDataOptionPathProblem : ITypeOption<int, DataOptionPathProblemBase>
{
    /// <summary>Says what happened to a rule's path, as the contract says it for the rule.</summary>
    /// <param name="logger">The logger the message is written to.</param>
    /// <param name="details">The details of the option layer's failure.</param>
    /// <param name="path">The path's segments.</param>
    /// <param name="text">The path as the rule states it.</param>
    /// <returns>The message.</returns>
    IGenericMessage Message(ILogger logger, IResultDetails details, IReadOnlyList<string> path, string text);
}
