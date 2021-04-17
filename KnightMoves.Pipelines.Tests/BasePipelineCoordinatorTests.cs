using KnightMoves.Pipelines.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace KnightMoves.Pipelines.Tests
{
    internal class TestPipelineContext : PipelineContext { }

    internal class TestBasePipelineCoordinator : BasePipelineCoordinator<TestPipelineContext>
    {
        public TestBasePipelineCoordinator(
            IReadOnlyDictionary<Type, IPipelineOperation<TestPipelineContext>> operations,
            IReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>> asyncOperations)
            : base(operations, asyncOperations)
        {

        }
    }

    internal class TestOperation : BasePipelineOperation<TestPipelineContext>
    {
        public TestOperation()
        {
            Dependencies.Add(typeof(TestDependencyOperation));
        }

        public override void Execute(TestPipelineContext context) { }
    }

    internal class TestDependencyOperation : BasePipelineOperation<TestPipelineContext>
    {
        public override void Execute(TestPipelineContext context) { }
    }

    internal class TestOperationAsync : BasePipelineOperationAsync<TestPipelineContext>
    {
        public TestOperationAsync()
        {
            Dependencies.Add(typeof(TestDependencyOperation));
        }

        public override Task ExecuteAsync() => Task.CompletedTask;

        public override void CompletedTaskCallback(object task) { }
    }

    internal class TestDependencyOperationAsync : BasePipelineOperationAsync<TestPipelineContext>
    {
        public override Task ExecuteAsync() => Task.CompletedTask;

        public override void CompletedTaskCallback(object task) { }
    }

    public class BasePipelineCoordinatorTests
    {
        private readonly IReadOnlyDictionary<Type, IPipelineOperation<TestPipelineContext>> _operations;
        private readonly IReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>> _asyncOperations;

        public BasePipelineCoordinatorTests()
        {
            _operations = new ReadOnlyDictionary<Type, IPipelineOperation<TestPipelineContext>>(new Dictionary<Type, IPipelineOperation<TestPipelineContext>>());
            _asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>());
        }

        [Fact]
        public void OperationsExecuted_Property_IsNotNull()
        {
            // ARRANGE / ACTION
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            // ASSERT
            Assert.NotNull(opManager.OperationsExecuted);
        }

        [Fact]
        public void OperationTasks_Property_IsNotNull()
        {
            // ARRANGE / ACTION
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            // ASSERT
            Assert.NotNull(opManager.OperationTasks);
        }

        [Fact]
        public void Execute_ThrowsArgExceptionOnNullContextProperty()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            opManager.Context = null;

            // ACTION / ASSERT
            Assert.Throws<ArgumentException>(() => opManager.Execute<TestOperation>());
        }

        [Fact]
        public void Execute_ThrowsExceptionOnNullContextParameter()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            // ACTION / ASSERT
            Assert.Throws<ArgumentException>(() => opManager.Execute<TestOperation>(null));
        }

        [Fact]
        public void Execute_ThrowsExceptionOnMissingDependency()
        {
            // ARRANGE
            var testOperation = new TestOperation();

            var operations = new ReadOnlyDictionary<Type, IPipelineOperation<TestPipelineContext>>(
                                    new Dictionary<Type, IPipelineOperation<TestPipelineContext>>
                                    {
                                        { typeof(TestOperation), testOperation }
                                    }
                                 );

            var opManager = new TestBasePipelineCoordinator(operations, _asyncOperations);

            // ACTION
            opManager.Execute<TestOperation>();

            // ASSERT
            Assert.True(opManager.Context.EndProcessing);
            Assert.Single(opManager.Context.Exceptions);
        }

        [Fact]
        public void Execute_DoesNothingWhenEndProcessingInContextIsTrue()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            opManager.Context.EndProcessing = true;

            // ACTION
            opManager.Execute<TestDependencyOperation>();

            // ASSERT
            Assert.True(opManager.Context.Successful);
            Assert.Empty(opManager.Context.Exceptions);
            Assert.Empty(opManager.OperationsExecuted);
            Assert.Single(opManager.Context.ResultMessages);
            Assert.Contains("Not Executed", opManager.Context.ResultMessages[0]);
        }

        [Fact]
        public void Execute_HandlesExceptionThrownByOperation()
        {
            // ARRANGE
            var mockOperation = new Mock<IPipelineOperation<TestPipelineContext>>();

            mockOperation
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation
                .Setup(o => o.Execute(It.IsAny<TestPipelineContext>()))
                .Throws<OperationExecutionException>();

            var operations = new ReadOnlyDictionary<Type, IPipelineOperation<TestPipelineContext>>(
                                    new Dictionary<Type, IPipelineOperation<TestPipelineContext>>
                                    {
                                        { typeof(TestOperation), mockOperation.Object }
                                    }
                                 );

            var opManager = new TestBasePipelineCoordinator(operations, _asyncOperations);

            // ACTION
            opManager.Execute<TestOperation>();

            // ASSERT
            Assert.False(opManager.Context.Successful);
            Assert.NotEmpty(opManager.Context.Exceptions);
            Assert.IsType<OperationExecutionException>(opManager.Context.Exceptions[0]);
            Assert.Contains(typeof(TestOperation), opManager.OperationsExecuted);
        }

        [Fact]
        public void Execute_RunsOperationSuccessfully()
        {
            // ARRANGE
            var operations = new ReadOnlyDictionary<Type, IPipelineOperation<TestPipelineContext>>(
                                    new Dictionary<Type, IPipelineOperation<TestPipelineContext>>
                                    {
                                        { typeof(TestDependencyOperation), new TestDependencyOperation() }
                                    }
                                 );

            var opManager = new TestBasePipelineCoordinator(operations, _asyncOperations);

            // ACTION
            opManager.Execute<TestDependencyOperation>();

            // ASSERT
            Assert.True(opManager.Context.Successful);
            Assert.Empty(opManager.Context.Exceptions);
            Assert.Contains(typeof(TestDependencyOperation), opManager.OperationsExecuted);
        }

        [Fact]
        public void ExecuteAsync_ThrowsArgExceptionOnNullContextProperty()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            opManager.Context = null;

            // ACTION / ASSERT
            Assert.Throws<ArgumentException>(() => opManager.ExecuteAsync<TestOperationAsync>());
        }

        [Fact]
        public void ExecuteAsync_ThrowsExceptionOnNullContextParameter()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            // ACTION / ASSERT
            Assert.Throws<ArgumentException>(() => opManager.ExecuteAsync<TestOperationAsync>(null));
        }

        [Fact]
        public void ExecuteAsync_DoesNothingWhenEndProcessingInContextIsTrue()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            opManager.Context.EndProcessing = true;

            // ACTION
            opManager.ExecuteAsync<TestDependencyOperationAsync>();

            // ASSERT
            Assert.True(opManager.Context.Successful);
            Assert.Empty(opManager.Context.Exceptions);
            Assert.Empty(opManager.OperationsExecuted);
            Assert.Single(opManager.Context.ResultMessages);
            Assert.Contains("Not Executed", opManager.Context.ResultMessages[0]);
        }

        [Fact]
        public void ExecuteAsync_ThrowsOpExceptionWhenOtherTasksArePending()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            var task = Task.CompletedTask;

            opManager.OperationTasks.Add(task);

            // ACTION / ASSERT
            Assert.Throws<OperationExecutionException>(() => opManager.ExecuteAsync<TestOperationAsync>());
        }

        [Fact]
        public void ExecuteAsync_ThrowsExceptionOnMissingDependency()
        {
            // ARRANGE
            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestOperationAsync), new TestOperationAsync() }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager.ExecuteAsync<TestOperationAsync>();

            // ASSERT
            Assert.True(opManager.Context.EndProcessing);
            Assert.Single(opManager.Context.Exceptions);
        }

        [Fact]
        public void ExecuteAsync_HandlesExceptionThrownInOperationExecuteAsyncMethod()
        {
            // ARRANGE
            var mockOperation = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation
                .Setup(o => o.ExecuteAsync())
                .ThrowsAsync(new OperationExecutionException("Unit test exception"));

            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestOperationAsync), mockOperation.Object }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager.ExecuteAsync<TestOperationAsync>();

            // ASSERT
            Assert.False(opManager.Context.Successful);
            Assert.NotEmpty(opManager.Context.Exceptions);
        }

        [Fact]
        public void ExecuteAsync_HandlesExceptionThrownInOperationTask()
        {
            // ARRANGE
            var mockOperation = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation
                .Setup(o => o.ExecuteAsync())
                .Returns(Task.FromException(new OperationExecutionException("Unit test exception")));

            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestOperationAsync), mockOperation.Object }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager.ExecuteAsync<TestOperationAsync>();

            // ASSERT
            Assert.False(opManager.Context.Successful);
            Assert.NotEmpty(opManager.Context.Exceptions);
        }

        [Fact]
        public void ExecuteAsync_HandlesExceptionThrownInCompletedTaskCallback()
        {
            // ARRANGE
            var mockOperation = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation
                .Setup(o => o.ExecuteAsync())
                .Returns(Task.CompletedTask);

            mockOperation
                .Setup(o => o.CompletedTaskCallback(It.IsAny<Task>()))
                .Throws(new OperationExecutionException("Unit test exception"));

            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestOperationAsync), mockOperation.Object }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager.ExecuteAsync<TestOperationAsync>();

            // ASSERT
            Assert.False(opManager.Context.Successful);
            Assert.NotEmpty(opManager.Context.Exceptions);
            Assert.IsType<OperationExecutionException>(opManager.Context.Exceptions[0]);
            Assert.Contains(typeof(TestOperationAsync), opManager.OperationsExecuted);
        }

        [Fact]
        public void ExecuteAsync_RunsOperationSuccessfully()
        {
            // ARRANGE
            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestDependencyOperationAsync), new TestDependencyOperationAsync() }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager.ExecuteAsync<TestDependencyOperationAsync>();

            // ASSERT
            Assert.True(opManager.Context.Successful);
            Assert.Empty(opManager.Context.Exceptions);
            Assert.Contains(typeof(TestDependencyOperationAsync), opManager.OperationsExecuted);
        }

        [Fact]
        public void AddAsyncOperation_ThrowsArgExceptionOnNullContextProperty()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            opManager.Context = null;

            // ACTION / ASSERT
            Assert.Throws<ArgumentException>(() => opManager.AddAsyncOperation<TestOperationAsync>());
        }

        [Fact]
        public void AddAsyncOperation_ThrowsArgExceptionOnNullContextParameter()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            // ACTION / ASSERT
            Assert.Throws<ArgumentException>(() => opManager.AddAsyncOperation<TestOperationAsync>(null));
        }

        [Fact]
        public void AddAsyncOperation_DoesNothingWhenEndProcessingInContextIsTrue()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            opManager.Context.EndProcessing = true;

            // ACTION
            opManager.AddAsyncOperation<TestDependencyOperationAsync>();

            // ASSERT
            Assert.True(opManager.Context.Successful);
            Assert.Empty(opManager.Context.Exceptions);
            Assert.Empty(opManager.OperationsExecuted);
            Assert.Single(opManager.Context.ResultMessages);
            Assert.Contains("Not Executed", opManager.Context.ResultMessages[0]);
        }

        [Fact]
        public void AddAsyncOperation_ThrowsExceptionOnMissingDependency()
        {
            // ARRANGE
            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestOperationAsync), new TestOperationAsync() }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager.AddAsyncOperation<TestOperationAsync>();

            // ASSERT
            Assert.True(opManager.Context.EndProcessing);
            Assert.Single(opManager.Context.Exceptions);
        }

        [Fact]
        public void AddAsyncOperation_HandlesExceptionThrownInCompletedTaskCallback()
        {
            // ARRANGE
            var mockOperation = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation
                .Setup(o => o.ExecuteAsync())
                .Returns(Task.CompletedTask);

            mockOperation
                .Setup(o => o.CompletedTaskCallback(It.IsAny<Task>()))
                .Throws(new OperationExecutionException("Unit test exception"));

            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestOperationAsync), mockOperation.Object }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager
                .AddAsyncOperation<TestOperationAsync>()
                .WhenAll();

            // ASSERT
            Assert.False(opManager.Context.Successful);
            Assert.NotEmpty(opManager.Context.Exceptions);
            Assert.IsType<OperationExecutionException>(opManager.Context.Exceptions[0]);
            Assert.Contains(typeof(TestOperationAsync), opManager.OperationsExecuted);
        }

        [Fact]
        public void WhenAll_DoesNothingWhenEndProcessingInContextIsTrue()
        {
            // ARRANGE
            var opManager = new TestBasePipelineCoordinator(_operations, _asyncOperations);

            opManager.Context.EndProcessing = true;

            // ACTION
            opManager.WhenAll();

            // ASSERT
            Assert.True(opManager.Context.Successful);
            Assert.Empty(opManager.Context.Exceptions);
            Assert.Empty(opManager.OperationsExecuted);
        }

        [Fact]
        public void WhenAll_AwaitsPendingTasksInContextProperty()
        {
            // ARRANGE
            var mockOperation1 = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation1
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation1
                .Setup(o => o.ExecuteAsync())
                .Returns(() => Task.CompletedTask);

            var mockOperation2 = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation2
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation2
                .Setup(o => o.ExecuteAsync())
                .Returns(() => Task.CompletedTask);

            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestDependencyOperationAsync), mockOperation1.Object },
                                            { typeof(TestOperationAsync), mockOperation2.Object }
                                        }
                                      );

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager
                .AddAsyncOperation<TestDependencyOperationAsync>()
                .AddAsyncOperation<TestOperationAsync>()
                .WhenAll();

            // ASSERT
            Assert.True(opManager.Context.Successful);
            Assert.Empty(opManager.Context.Exceptions);
            Assert.Contains(typeof(TestDependencyOperationAsync), opManager.OperationsExecuted);
            Assert.Contains(typeof(TestOperationAsync), opManager.OperationsExecuted);
        }

        [Fact]
        public void WhenAll_AwaitsPendingTasksInContextParameter()
        {
            // ARRANGE
            var mockOperation1 = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation1
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation1
                .Setup(o => o.ExecuteAsync())
                .Returns(() => Task.CompletedTask);

            var mockOperation2 = new Mock<IPipelineOperationAsync<TestPipelineContext>>();

            mockOperation2
                .Setup(o => o.Dependencies)
                .Returns(new List<Type>());

            mockOperation2
                .Setup(o => o.ExecuteAsync())
                .Returns(() => Task.CompletedTask);

            var asyncOperations = new ReadOnlyDictionary<Type, IPipelineOperationAsync<TestPipelineContext>>(
                                        new Dictionary<Type, IPipelineOperationAsync<TestPipelineContext>>
                                        {
                                            { typeof(TestDependencyOperationAsync), mockOperation1.Object },
                                            { typeof(TestOperationAsync), mockOperation2.Object }
                                        }
                                      );

            var context = new TestPipelineContext();

            var opManager = new TestBasePipelineCoordinator(_operations, asyncOperations);

            // ACTION
            opManager
                .AddAsyncOperation<TestDependencyOperationAsync>(context)
                .AddAsyncOperation<TestOperationAsync>(context)
                .WhenAll(context);

            // ASSERT
            Assert.True(context.Successful);
            Assert.Empty(context.Exceptions);
            Assert.Contains(typeof(TestDependencyOperationAsync), opManager.OperationsExecuted);
            Assert.Contains(typeof(TestOperationAsync), opManager.OperationsExecuted);
        }
    }
}
