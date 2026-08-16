using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Refactoring.Commands;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Translators;

[TypeOption(typeof(SqlRefactoringTranslators), "RenameTable", RestrictToCurrentCompilation = true)]
public sealed class RenameTableTranslator : SqlCommandTranslatorBase<RenameTableCommand, QueryResult<string>>
{
    public RenameTableTranslator() : base("RenameTable", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(RenameTableCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<RenameTableTranslator>.Instance;
        RenameTableTranslatorLog.Translating(logger, nameof(RenameTableCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(RenameTableTranslatorLog.NotYetImplemented(logger, nameof(RenameTableCommand))));
    }
}
