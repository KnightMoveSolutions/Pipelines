using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnightMoves.Pipelines.Interfaces;

namespace KnightMoves.Pipelines
{
    /// <summary>
    /// Default implementation of the <see cref="IPipelineCoordinator{TContext}"/> that serves as the builder and the manager 
    /// of operation pipelines. Applications implement this abstract class specifying the specific type of <typeparamref name="TContext"/>
    /// that will be used by the operations in its pipeline.
    /// </summary>
    /// <typeparam name="TContext">The type of state object that will be acted upon</typeparam>
    public abstract class BasePipelineCoordinator<TContext> : IPipelineCoordinator<TContext> where TContext : IPipelineContext, new()
    {
        /// <summary>
        /// The state object that each operation acts upon
        /// </summary>
        /// <remarks>
        /// The state object can be placed here. If this is null then the context object must be passed 
        /// to the Execute method that accepts it as a parameter in order for it to be acted upon.
        /// </remarks>
        public TContext Context { get; set; }

        /// <summary>
        /// A collection of <see cref="Type"/>s that have been executed
        /// </summary>
        /// <remarks>
        /// When <see cref="IPipelineOperation{TContext}"/> objects have been executed, their types are added to
        /// this collection so that other <see cref="IPipelineOperation{TContext}"/> objects can be checked to 
        /// see if their dependencies have been satisfied.
        /// </remarks>
        public List<Type> OperationsExecuted { get; } = new List<Type>();

        /// <summary>
        /// The list of <see cref="IPipelineOperation{TContext}"/> objects that this coordinator offers for execution
        /// </summary>
        public IReadOnlyDictionary<Type, IPipelineOperation<TContext>> Operations { get; set; }

        /// <summary>
        /// This list of <see cref="IPipelineOperationAsync{TContext}"/> objects that this coordinator offers for execution
        /// </summary>
        public IReadOnlyDictionary<Type, IPipelineOperationAsync<TContext>> AsyncOperations { get; set; }

        /// <summary>
        /// Contains the collection of Async Tasks that haven't been awaited yet.
        /// </summary>
        /// <remarks>
        /// The <see cref="WhenAll"/> method awaits this collection of Tasks and clears the collection when finished
        /// </remarks>
        public IList<Task> OperationTasks { get; set; } = new List<Task>();

        public BasePipelineCoordinator(IReadOnlyDictionary<Type, IPipelineOperation<TContext>> operations, IReadOnlyDictionary<Type, IPipelineOperationAsync<TContext>> asyncOperations)
        {
            Context = new TContext();
            Operations = operations;
            AsyncOperations = asyncOperations;
        }

        /// <summary>
        /// This method executes the <see cref="IPipelineOperation{TContext}.Execute(TContext)"/> method of the specified 
        /// <see cref="IPipelineOperation{TContext}"/> object passing it the <see cref="TContext"/> object from the 
        /// <see cref="Context"/> property.
        /// </summary>
        /// <typeparam name="TOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> Execute<TOperation>() where TOperation : IPipelineOperation<TContext> 
        {
            if (Context == null)
                throw new ArgumentException($"The {nameof(Context)} property cannot be null in order to use this method. Otherwise, use the {nameof(Execute)}<{nameof(TOperation)}>({nameof(TContext)} context) method and pass in the state object explicitly.");

            return Execute<TOperation>(Context);
        }

        /// <summary>
        /// This method executes the <see cref="IPipelineOperation{TContext}.Execute(TContext)"/> method of the specified
        /// <see cref="IPipelineOperation{TContext}"/> object passing it the <see cref="TContext"/> from the method parameter.
        /// </summary>
        /// <typeparam name="TOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <param name="context">The state object that will be acted upon</param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> Execute<TOperation>(TContext context) where TOperation : IPipelineOperation<TContext> 
        {
            if (context == null)
                throw new ArgumentException($"Parameter {typeof(TContext).Name} {nameof(context)} cannot be null", nameof(context));

            var opType = typeof(TOperation);

            if(context.EndProcessing)
            {
                context.ResultMessages.Add($"[{opType.Name}] Not Executed. A previous Operation terminated the operation pipeline.");
                return this;
            }

            var operation = Operations[opType];

            try
            {
                CheckDependencies(operation);
            }
            catch (OperationDependencyNotExecutedException ex)
            {
                context.EndProcessing = true;
                context.Exceptions.Add(ex);
                return this;
            }

            try
            {
                operation.Execute(context);
            }
            catch (Exception ex)
            {
                context.Successful = false;
                context.Exceptions.Add(ex);

                if (ex is OperationExecutionException && !OperationsExecuted.Contains(opType))
                    OperationsExecuted.Add(opType);

                return this;
            }

            if (!OperationsExecuted.Contains(opType))
                OperationsExecuted.Add(opType);

            return this;
        }

        /// <summary>
        /// This method executes the <see cref="IPipelineOperationAsync{TContext}.ExecuteAsync"/> method of the specified
        /// <see cref="IPipelineOperationAsync{TContext}"/> object passing it the <see cref="TContext"/> object 
        /// from the <see cref="Context"/> property.
        /// </summary>
        /// <typeparam name="TAsyncOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> ExecuteAsync<TAsyncOperation>() where TAsyncOperation : IPipelineOperationAsync<TContext>
        {
            if (Context == null)
                throw new ArgumentException($"The {nameof(Context)} property cannot be null in order to use this method. Otherwise, use the {nameof(ExecuteAsync)}<{nameof(TAsyncOperation)}>({nameof(TContext)} context) method and pass in the state object explicitly.");

            return ExecuteAsync<TAsyncOperation>(Context);
        }

        /// <summary>
        /// This method executes the <see cref="IPipelineOperationAsync{TContext}.ExecuteAsync"/> method of the specified
        /// <see cref="IPipelineOperationAsync{TContext}"/> object passing it the <see cref="TContext"/> from the method parameter 
        /// from the <see cref="Context"/> property.
        /// </summary>
        /// <typeparam name="TAsyncOperation">The object that will be used to process the <see cref="TContext"/> object</typeparam>
        /// <param name="context">The state object that will be acted upon</param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> ExecuteAsync<TAsyncOperation>(TContext context) where TAsyncOperation : IPipelineOperationAsync<TContext>
        {
            if (context == null)
                throw new ArgumentException($"Parameter {typeof(TContext).Name} {nameof(context)} cannot be null", nameof(context));

            if (OperationTasks.Any())
                throw new OperationExecutionException($"Cannot execute individual await Operation while other Operation Tasks are pending. Execute the {nameof(WhenAll)} method to clear the pending Operation Tasks first.");

            var opType = typeof(TAsyncOperation);

            if(context.EndProcessing)
            {
                context.ResultMessages.Add($"[{opType.Name}] Not Executed. A previous Operation terminated the operation pipeline.");
                return this;
            }

            var operation = AsyncOperations[opType];

            try
            {
                CheckDependencies(operation);
            }
            catch (OperationDependencyNotExecutedException ex)
            {
                context.EndProcessing = true;
                context.Exceptions.Add(ex);
                return this;
            }

            operation.Context = context;

            var operationTask = operation
                                    .ExecuteAsync()
                                    .ContinueWith(task =>
                                    {
                                        try
                                        {
                                            operation.CompletedTaskCallback(task);

                                            if (!OperationsExecuted.Contains(opType))
                                                OperationsExecuted.Add(opType);
                                        }
                                        catch (Exception ex)
                                        {
                                            context.Successful = false;
                                            context.Exceptions.Add(ex);

                                            if (ex is OperationExecutionException && !OperationsExecuted.Contains(opType))
                                                OperationsExecuted.Add(opType);
                                        }
                                    }, TaskContinuationOptions.OnlyOnRanToCompletion);

            try
            {
                operationTask.Wait();
            }
            catch (Exception ex)
            {
                context.Successful = false;
                context.Exceptions.Add(ex);

                if (ex is OperationExecutionException && !OperationsExecuted.Contains(opType))
                    OperationsExecuted.Add(opType);
            }

            return this;
        }

        /// <summary>
        /// Adds the object specified by the <typeparamref name="TAsyncOperation"/> generic type to the <see cref="OperationTasks"/>
        /// collection to be awaited later asynchronously by one of the <see cref="WhenAll"/> methods
        /// </summary>
        /// <typeparam name="TAsyncOperation">The type of async operation that implements <see cref="IPipelineOperationAsync{TContext}"/></typeparam>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> AddAsyncOperation<TAsyncOperation>() where TAsyncOperation : IPipelineOperationAsync<TContext>
        {
            if(Context == null)
                throw new ArgumentException($"The {nameof(Context)} property cannot be null in order to use this method. Otherwise, use the {nameof(ExecuteAsync)}<{nameof(TAsyncOperation)}>({nameof(TContext)} context) method and pass in the state object explicitly.");

            return AddAsyncOperation<TAsyncOperation>(Context);
        }

        /// <summary>
        /// Adds the object specified by the <typeparamref name="TAsyncOperation"/> generic type to the <see cref="OperationTasks"/>
        /// collection to be awaited later asynchronously by one of the <see cref="WhenAll"/> methods
        /// </summary>
        /// <typeparam name="TAsyncOperation">The type of async operation that implements <see cref="IPipelineOperationAsync{TContext}"/></typeparam>
        /// <param name="context">The state object that will be acted upon</param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> AddAsyncOperation<TAsyncOperation>(TContext context) where TAsyncOperation : IPipelineOperationAsync<TContext>
        {
            if (context == null)
                throw new ArgumentException($"Parameter {typeof(TContext).Name} {nameof(context)} cannot be null", nameof(context));

            var opType = typeof(TAsyncOperation);

            if (context.EndProcessing)
            {
                context.ResultMessages.Add($"[{opType.Name}] Not Executed. A previous Operation terminated the operation pipeline.");
                return this;
            }

            var operation = AsyncOperations[opType];

            try
            {
                CheckDependencies(operation);
            }
            catch (OperationDependencyNotExecutedException ex)
            {
                context.EndProcessing = true;
                context.Exceptions.Add(ex);
                return this;
            }

            operation.Context = context;

            OperationTasks.Add(

                operation

                    .ExecuteAsync()

                    .ContinueWith(
                        (task) =>
                        {
                            try
                            {
                                operation.CompletedTaskCallback(task);

                                if (!OperationsExecuted.Contains(opType))
                                    OperationsExecuted.Add(opType);
                            }
                            catch (Exception ex)
                            {
                                context.Successful = false;
                                context.Exceptions.Add(ex);

                                if (ex is OperationExecutionException && !OperationsExecuted.Contains(opType))
                                    OperationsExecuted.Add(opType);
                            }
                        },
                        TaskContinuationOptions.OnlyOnRanToCompletion
                    )

                    .ContinueWith(
                        (task) =>
                        {
                            context.Successful = false;
                            context.Exceptions.Add(task.Exception);
                        },
                        TaskContinuationOptions.OnlyOnFaulted
                    )

            );

            return this;
        }

        /// <summary>
        /// Calls <see cref="Task.WhenAll(Task[])"/> using an array of <see cref="Task"/>s derived from the <see cref="OperationTasks"/>
        /// collection in order to asynchronously await all of the pending tasks in the <see cref="OperationTasks"/> collection.
        /// </summary>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> WhenAll()
        {
            return WhenAll(Context);
        }

        /// <summary>
        /// Calls <see cref="Task.WhenAll(Task[])"/> using an array of <see cref="Task"/>s derived from the <see cref="OperationTasks"/>
        /// collection in order to asynchronously await all of the pending tasks in the <see cref="OperationTasks"/> collection.
        /// </summary>
        /// <param name="context">The state object that will be acted upon</param>
        /// <returns><see cref="IPipelineCoordinator{TContext}"/> for dot chaining</returns>
        public virtual IPipelineCoordinator<TContext> WhenAll(TContext context)
        {
            if (context.EndProcessing)
                return this;

            try
            {
                Task
                    .WhenAll(OperationTasks)
                    .IgnoreCancellation()
                    .Wait();
            }
            catch (AggregateException ex)
            {
                context.Successful = false;
                context.Exceptions.Add(ex.Flatten());
            }

            OperationTasks.Clear();

            return this;
        }

        protected virtual void CheckDependencies(IPipelineOperation<TContext> operation)
        {
            operation
                .Dependencies
                .ToList()
                .ForEach(d =>
                {
                    if(!OperationsExecuted.Contains(d))
                    {
                        var operationName = operation.GetType().Name;
                        var dependencyName = d.Name;
                        var message = $"The {operationName} operation could not execute because it requires that the {dependencyName} operation be executed before it";
                        throw new OperationDependencyNotExecutedException(message);
                    }
                });
        }
    }

    public static class TaskExtensions
    {
        public static Task IgnoreCancellation(this Task task)
        {
            return task.ContinueWith(t => { }, TaskContinuationOptions.OnlyOnCanceled);
        }
    }
}
