using Xunit;

// Disable test parallelization for this assembly.
// BuilderResultCodes TypeCollection has thread-unsafe lazy initialization
// that causes NullReferenceException when multiple test classes access it
// concurrently during parallel execution.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
