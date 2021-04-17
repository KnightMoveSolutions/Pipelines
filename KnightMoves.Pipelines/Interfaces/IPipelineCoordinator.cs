using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KnightMoves.Pipelines.Interfaces
{
    /// <summary>
    /// Classes that implement this interface will implement <see cref="Execute{TOperation}(TContext)"/> methods that
    /// will pass the <typeparamref name="TContext"/> state object to the TOperation object that implements 
    /// <see cref="IPipelineOperation{TContext}"/>
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are meant to be the head of a Chain of Responsibility Pattern where the order
    /// of execution matters and where the order of execution is explicitly defined by the order in which the 
    /// <see cref="Execute{TOperation}(TContext)"/> methods are called. Calls to the <see cref="Execute{TOperation}(TContext)"/>
    /// methods can be "dot chained" for clarity.
    /// </remarks>
    /// <typeparam name="TContext">The type of state object that will be acted upon</typeparam>
    public interface IPipelineCoordinator<TContext> where TContext : IPipelineContext 
    {
        /// <summary>
        /// The state object that each Operation acts upon
        /// </summary>
        /// <remarks>
        /// The state object can be placed here. If this is null then the context object must be passed to the Execute method 
        /// that accepts it as a prameter in order for it to be acted upon.
        /// </remarks>
        TContext Context { get; set; }

        /// <summary>
        /// The list of <see cref="IPipelineOperation{TContext}"/> objects that this coordinator offers for execution
        /// </summary>
        IReadOnlyDictionary<Type, IPipelineOperation<TContext>> Operations { get; set; }

        /// <summary>
        /// A collection of <see cref="Type"/>s that have been executed.
        /// </summary>
        /// <remarks>
        /// Implementations of this interface must ensure that when <see cref="IPipelineOperation{TContext}"/>
        /// objects have been executed that their types are added to this collection so that other 
        /// <see cref="IPipelineOperation{TContext}"/> objects can be checked to see if their dependencies 
        /// have been satisfied.
        /// </remarks>
        List<Type> OperationsExecuted { get; }

        /// <summary>
        /// This method executes the <see cref="Execute{TOperation}(TContext)"/> method of the specified <see cref="IPipelineOperation{TContext}"/>
        /// object passing it the <see cref="TContext"/> object from the <see cref="TContext"/> property.
        /// </summary>
        /// <remarks>
        /// it is assumed that the class that implements this method will indeed pass the <see cref="Context"/> property object to the operation.
        /// Implementors of this interface must do so in order to avoid a violation of the Liskov Substitution Principle.
        /// </remarks>
        /// <typeparam name="TOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> Execute<TOperation>() where TOperation : IPipelineOperation<TContext>;

        /// <summary>
        /// This method executes the <see cref="Execute{TOperation}(TContext)"/> method of the specified <see cref="IPipelineOperation{TContext}"/>
        /// object passing it the <see cref="TContext"/> from the method parameter.
        /// </summary>
        /// <remarks>
        /// it is assumed that the class that implements this method will indeed pass the provided <see cref="TContext"/> object to the operation.
        /// Implementors of this interface must do so in order to avoid a violation of the Liskov Substitution Principle.
        /// </remarks>
        /// <typeparam name="TOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <param name="context"></param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> Execute<TOperation>(TContext context) where TOperation : IPipelineOperation<TContext>;

        /// <summary>
        /// The list of Async Operations that this coordinator offers for execution
        /// </summary>
        IReadOnlyDictionary<Type, IPipelineOperationAsync<TContext>> AsyncOperations { get; set; }

        /// <summary>
        /// Contains the collection of Async Tasks that haven't been awaited yet
        /// </summary>
        /// <remarks>
        /// The <see cref="WhenAll"/> method is expected to await this collection of Tasks and clear the collection when finished
        /// </remarks>
        IList<Task> OperationTasks { get; set; }

        /// <summary>
        /// This method executes the <see cref="IPipelineOperationAsync{TContext}.ExecuteAsync"/> method of the specified 
        /// <see cref="IPipelineOperationAsync{TContext}"/> object passing it the <see cref="TContext"/> object from the
        /// Context property.
        /// </summary>
        /// <remarks>
        /// it is assumed that the class that implements this method will indeed pass the <see cref="Context"/> property object to the operation.
        /// Implementors of this interface must do so in order to avoid a violation of the Liskov Substitution Principle.
        /// </remarks>
        /// <typeparam name="TAsyncOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> ExecuteAsync<TAsyncOperation>() where TAsyncOperation : IPipelineOperationAsync<TContext>;

        /// <summary>
        /// This method executes the <see cref="IPipelineOperationAsync{TContext}.ExecuteAsync"/> method of the specified 
        /// <see cref="IPipelineOperationAsync{TContext}"/> object passing it the <see cref="TContext"/> object from the
        /// method parameter.
        /// </summary>
        /// <remarks>
        /// it is assumed that the class that implements this method will indeed pass the provided <see cref="TContext"/> object to the operation.
        /// Implementors of this interface must do so in order to avoid a violation of the Liskov Substitution Principle.
        /// </remarks>
        /// <typeparam name="TAsyncOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <param name="context">The state object that will be acted upon</param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> ExecuteAsync<TAsyncOperation>(TContext context) where TAsyncOperation : IPipelineOperationAsync<TContext>;

        /// <summary>
        /// Adds the object specified by the <typeparamref name="TAsyncOperation"/> generic type to the <see cref="OperationTasks"/>
        /// collection to be awaited later asynchronously by one of the <see cref="WhenAll"/> methods.
        /// </summary>
        /// <remarks>
        /// it is assumed that the class that implements this method will indeed pass the <see cref="Context"/> property
        /// object to the operation. Implementors of this interface must do so in order to avoid a violation of the Liskov
        /// Substitution Principle.
        /// </remarks>
        /// <typeparam name="TAsyncOperation">The type of operation that implements <see cref="IPipelineOperationAsync{TContext}"/></typeparam>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> AddAsyncOperation<TAsyncOperation>() where TAsyncOperation : IPipelineOperationAsync<TContext>;

        /// <summary>
        /// Adds the object specified by the <typeparamref name="TAsyncOperation"/> generic type to the <see cref="OperationTasks"/>
        /// collection to be awaited later asynchronously by one of the <see cref="WhenAll"/> methods.
        /// </summary>
        /// <remarks>
        /// it is assumed that the class that implements this method will indeed pass the provided <see cref="TContext"/> 
        /// object to the operation. Implementors of this interface must do so in order to avoid a violation of the Liskov
        /// Substitution Principle.
        /// </remarks>
        /// <typeparam name="TAsyncOperation">The type of operation that implements <see cref="IPipelineOperationAsync{TContext}"/></typeparam>
        /// <param name="context">The state object that will be acted upon</param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> AddAsyncOperation<TAsyncOperation>(TContext context) where TAsyncOperation : IPipelineOperationAsync<TContext>;

        /// <summary>
        /// Calls <see cref="Task.WhenAll(Task[])"/> using an array of <see cref="Task"/>s derived from the <see cref="OperationTasks"/>
        /// collection in order to asynchronously await all of the pending tasks in the <see cref="OperationTasks"/> collection.
        /// </summary>
        /// <remarks>
        /// It is assumed that the class that implements this method will clear the <see cref="OperationTasks"/> collection whether or 
        /// not the tasks ran to completion. Exception handling is encouraged but not assumbed to be part of the implementation. It is 
        /// also assumed that this method will use the state object in the <see cref="Context"/> property as it sees fit.
        /// </remarks>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> WhenAll();

        /// <summary>
        /// Calls <see cref="Task.WhenAll(Task[])"/> using an array of <see cref="Task"/>s derived from the <see cref="OperationTasks"/>
        /// collection in order to asynchronously await all of the pending tasks in the <see cref="OperationTasks"/> collection.
        /// </summary>
        /// <remarks>
        /// It is assumed that the class that implements this method will clear the <see cref="OperationTasks"/> collection whether or 
        /// not the tasks ran to completion. Exception handling is encouraged but not assumbed to be part of the implementation. It is 
        /// also assumed that this method will use the state object in the <typeparamref name="TContext"/> parameter as it sees fit.
        /// </remarks>
        /// <param name="context">The state object that will be acted upon</param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        IPipelineCoordinator<TContext> WhenAll(TContext context);
    }
}
