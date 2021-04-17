using System.Threading.Tasks;

namespace KnightMoves.Pipelines.Interfaces
{
    public interface IPipelineOperationAsync<TContext> : IPipelineOperation<TContext> where TContext : IPipelineContext
    {
        /// <summary>
        /// The state object that this operation will act upon
        /// </summary>
        TContext Context { get; set; }

        /// <summary>
        /// The method that executes the logic of the operation and returns a <see cref="Task"/> to be
        /// awaited later by the <see cref="IPipelineCoordinator{TContext}"/>
        /// </summary>
        /// <returns></returns>
        Task ExecuteAsync();

        /// <summary>
        /// The callback method to be executed after the <see cref="Task"/> from the <see cref="ExecuteAsync"/> method has been awaited.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> object from the <see cref="ExecuteAsync"/> method after it's been awaited</param>
        void CompletedTaskCallback(object task);
    }
}
