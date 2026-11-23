using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;
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

/// <summary>The ways the option layer refuses a rule's path, each found by the code it fails with.</summary>
[TypeCollection(typeof(DataOptionPathProblemBase), typeof(IDataOptionPathProblem), typeof(DataOptionPathProblems))]
[ExcludeFromCodeCoverage]
public abstract partial class DataOptionPathProblems : TypeCollectionBase<DataOptionPathProblemBase, IDataOptionPathProblem>
{
}
