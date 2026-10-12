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

[TypeOption(typeof(SqlRefactoringTranslators), "ExtractView", RestrictToCurrentCompilation = true)]
public sealed class ExtractViewTranslator : SqlCommandTranslatorBase<ExtractViewCommand, QueryResult<string>>
{
    public ExtractViewTranslator() : base("ExtractView", "Stub. Full implementation pending.") { }

    public override Task<IGenericResult<QueryResult<string>>> Translate(ExtractViewCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<ExtractViewTranslator>.Instance;
        ExtractViewTranslatorLog.Translating(logger, nameof(ExtractViewCommand));
        return Task.FromResult<IGenericResult<QueryResult<string>>>(
            GenericResult<QueryResult<string>>.Failure(ExtractViewTranslatorLog.NotYetImplemented(logger, nameof(ExtractViewCommand))));
    }
}
