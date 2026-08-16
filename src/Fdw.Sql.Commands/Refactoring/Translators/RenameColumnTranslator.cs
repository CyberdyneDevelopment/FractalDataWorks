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

[TypeOption(typeof(SqlRefactoringTranslators), "RenameColumn", RestrictToCurrentCompilation = true)]
public sealed class RenameColumnTranslator : SqlCommandTranslatorBase<RenameColumnCommand, QueryResult<string>>
{
    public RenameColumnTranslator() : base("RenameColumn", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(RenameColumnCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<RenameColumnTranslator>.Instance;
        RenameColumnTranslatorLog.Translating(logger, nameof(RenameColumnCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(RenameColumnTranslatorLog.NotYetImplemented(logger, nameof(RenameColumnCommand))));
    }
}
