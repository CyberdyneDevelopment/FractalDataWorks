using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Workspace.Commands;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Workspace.Translators;

[TypeOption(typeof(SqlWorkspaceTranslators), "CompareToBaseline", RestrictToCurrentCompilation = true)]
public sealed class CompareToBaselineTranslator : SqlCommandTranslatorBase<CompareToBaselineCommand, QueryResult<ComparisonInfo>>
{
    public CompareToBaselineTranslator() : base("CompareToBaseline", "Diffs current vs baseline.") { }

    public override Task<IGenericResult<QueryResult<ComparisonInfo>>> Translate(
        CompareToBaselineCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        if (workspace.BaselineModel is null)
            return Task.FromResult<IGenericResult<QueryResult<ComparisonInfo>>>(
                GenericResult<QueryResult<ComparisonInfo>>.Success(
                    new QueryResult<ComparisonInfo>("No baseline set", new ComparisonInfo { HasBaseline = false })));

        // Real diff is per-script text compare, which lives in SqlWorkspace.
        // Stub: just report the script count for now.
        var info = new ComparisonInfo { HasBaseline = true, ChangeCount = 0 };
        return Task.FromResult<IGenericResult<QueryResult<ComparisonInfo>>>(
            GenericResult<QueryResult<ComparisonInfo>>.Success(new QueryResult<ComparisonInfo>("Diff not yet computed", info)));
    }
}
