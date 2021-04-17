using System;
using System.Collections.Generic;

namespace KnightMoves.Pipelines.Interfaces
{
    /// <summary>
    /// Classes that implement this interface use the <typeparamref name="TContext"/> state object for processing.
    /// It can use the data in the object in its logic (read only) and/or it can modify data in the state object
    /// by adding or changing data therein before the enxt <see cref="IPipelineOperation{TContext}"/> object gets
    /// hold of it
    /// </summary>
    /// <typeparam name="TContext">The type of state object that will be acted upon</typeparam>
    public interface IPipelineOperation<TContext> where TContext : IPipelineContext 
    {
        /// <summary>
        /// A collection of <see cref="Type"/> objects representing the <see cref="IPipelineOperation{TContext}"/> operations
        /// that this object depends on.
        /// </summary>
        /// <remarks>
        /// If there are any <see cref="IPipelineOperation{TContext}"/> objects that must be executed before this object executes, 
        /// then this object depends on the execution of those other operations. One or more dependencies can be added to this
        /// collection in order to define what must have been executed before it. An empty collection means that it is not 
        /// dependent on any other operation.
        /// </remarks>
        List<Type> Dependencies { get; }

        /// <summary>
        /// The method that executes the logic of the operation
        /// </summary>
        /// <param name="context"></param>
        void Execute(TContext context);
    }
}
