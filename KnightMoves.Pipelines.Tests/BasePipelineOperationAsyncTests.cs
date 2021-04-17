using System.Threading.Tasks;
using Xunit;

namespace KnightMoves.Pipelines.Tests
{
    public class TestBasePipelineOperationAsync : BasePipelineOperationAsync<PipelineContext>
    {
        public override Task ExecuteAsync() { return Task.CompletedTask; }

        public override void CompletedTaskCallback(object task) { }
    }

    public class BasePipelineOperationAsyncTests
    {
        [Fact]
        public void ExecuteAsync_ReturnsCompletedTask()
        {
            // ARRANGE
            var testOp = new TestBasePipelineOperationAsync();

            // ACTION
            var task = testOp.ExecuteAsync();

            task.Wait();

            // ASSERT
            Assert.NotNull(task);
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }
    }
}
