using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Sql.Tests.Fakes;

/// <summary>A translator that records the key-list scope it is run in, so a test can see which setting reached it.</summary>
internal sealed class ScopeRecordingTranslator : SqlDataCommandTranslatorBase<object>
{
    public ScopeRecordingTranslator() : base("ScopeRecording", "Fake") { }

    public TranslationSettings? Seen { get; private set; }

    public bool RanInAScope { get; private set; }

    public override Task<IGenericResult<object>> Translate(IDataCommand command, IStorageContainer container, CancellationToken cancellationToken = default)
    {
        var scope = KeyListSpillScope.Current;
        RanInAScope = scope is not null;
        Seen = scope?.Settings;
        return Task.FromResult(GenericResult<object>.Success(new object()));
    }
}
