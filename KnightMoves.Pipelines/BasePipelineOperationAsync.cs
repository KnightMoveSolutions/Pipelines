using System.Threading.Tasks;
using KnightMoves.Pipelines.Interfaces;

namespace KnightMoves.Pipelines
{
    public abstract class BasePipelineOperationAsync<TContext> : BasePipelineOperation<TContext>, IPipelineOperationAsync<TContext> where TContext : IPipelineContext
    {
        public TContext Context { get; set; }

        /// <summary>
        /// The method that executes the logic of the operation and returns a <see cref="Task"/> to be
        /// awaited later by the <see cref="IPipelineCoordinator{TContext}"/>
        /// </summary>
        /// <remarks>
        /// The <paramref name="context"/> is saved in the <see cref="Context"/> property
        /// </remarks>
        /// <param name="context"></param>
        public override void Execute(TContext context)
        {
            Context = context;

            ExecuteAsync()
                .Wait();
        }

        public abstract Task ExecuteAsync();

        public abstract void CompletedTaskCallback(object task);
    }
}
