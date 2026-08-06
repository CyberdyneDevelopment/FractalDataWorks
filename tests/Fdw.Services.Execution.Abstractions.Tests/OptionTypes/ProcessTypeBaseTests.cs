using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Execution.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.OptionTypes;

public class ProcessTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsIdAndName()
    {
        // Arrange & Act
        var processType = new TestableProcessType(1, "TestType");

        // Assert
        processType.Id.ShouldBe(1);
        processType.Name.ShouldBe("TestType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ProcessTypeInheritsFromTypeOptionBase()
    {
        // Arrange & Act
        var processType = new TestableProcessType(1, "Test");

        // Assert
        processType.ShouldBeAssignableTo<IProcessType>();
    }

    private sealed class TestableProcessType : ProcessTypeBase
    {
        public TestableProcessType(int id, string name) : base(id, name)
        {
        }

        public override IProcess CreateProcess(string processId, object configuration, IServiceProvider serviceProvider)
        {
            throw new NotSupportedException("Test implementation");
        }

        public override Task<IProcessResult> Execute(string operationName, string processId, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Test implementation");
        }

        public override string[] GetSupportedOperations()
        {
            return new[] { "TestOperation" };
        }

        public override Type GetConfigurationType()
        {
            return typeof(object);
        }

        public override bool IsValidConfiguration(object configuration)
        {
            return configuration != null;
        }
    }
}
