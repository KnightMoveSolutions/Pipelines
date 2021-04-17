using Xunit;

namespace KnightMoves.Pipelines.Tests
{
    public class TestBasePipelineOperation : BasePipelineOperation<PipelineContext>
    {
        public override void Execute(PipelineContext context)
        {
            
        }
    }

    public class BasePipelineOperationTests
    {
        [Fact]
        public void Dependencies_Property_IsNotNull()
        {
            // ARRANGE / ACTION 
            var testOp = new TestBasePipelineOperation();

            // ASSERT
            Assert.NotNull(testOp);
        }
    }
}
