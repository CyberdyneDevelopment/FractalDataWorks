using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-1220: a formula read back is exactly the formula the request stated, piece for piece.</summary>
public sealed class BuiltFormulaReaderTests
{
    [Fact]
    public void AFormulaMappedToTheEnginesTreeAndReadBackIsTheFormulaStated()
    {
        var stated = new BuiltFormulaNodePayload
        {
            Op = "Round",
            Places = 2,
            Mode = "AwayFromZero",
            Operands =
            [
                new BuiltFormulaNodePayload
                {
                    Op = "Multiply",
                    Operands =
                    [
                        new BuiltFormulaNodePayload { Op = "Field", Name = "price" },
                        new BuiltFormulaNodePayload { Op = "Constant", Value = 1.5m },
                    ],
                },
            ],
        };

        var tree = BuiltFormulaMapper.ToNode("total", stated, 32, NullLogger.Instance);
        var read = BuiltFormulaReader.ToPayload(tree.Value!);

        read.Op.ShouldBe("Round");
        read.Places.ShouldBe(2);
        read.Mode.ShouldBe("AwayFromZero");
        read.Operands[0].Op.ShouldBe("Multiply");
        read.Operands[0].Operands[0].Name.ShouldBe("price");
        read.Operands[0].Operands[1].Value.ShouldBe(1.5m);
    }
}
