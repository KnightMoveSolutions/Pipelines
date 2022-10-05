`Documentation Home <https://docs.knightmovesolutions.com>`_

===========
Quick Start
===========

Step 1: Create the Context
--------------------------

Create a ``PipelineContext`` class for your pipeline

This is a POCO that inherits from ``PipelineContext`` and implements the ``IPipelineContext`` interface

.. code-block:: csharp
   :linenos:
   
   public class MyApplicationContext : PipelineContext, IPipelineContext
   {
        // Add your properties
   }

Step 2: Create the Coordinator
------------------------------

Create the ``PipelineCoordinator`` class.

Your Pipeline Coordinator class simply inherits from ``BasePipelineCoordinator<TContext>`` and 
provides the following:

* It specifies the type of ``PipelineContext`` object that will be processed. In this example it is 
  and object of type ``MyApplicationContext``

* It accepts two ``IReadOnlyDictionary<TKey, TValue>`` collections that are automatically injected by 
  the DI container for you. Your Pipeline Coordinator class simply accepts the injected collections
  and passes them to the base. Nothing further is required with those collections.
  
* The ``Context`` property must be initialized. In this example it is the line
  ``Context = new MyApplicationContext``
  
.. code-block:: csharp
   :linenos:
   
   using System;
   using System.Collections.Generic;
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces; 

   public class MyPipelineCoordinator : BasePipelineCoordinator<MyApplicationContext>
   {
        public MyPipelineCoordinator(
            IReadOnlyDictionary<Type, IPipelineOperation<MyApplicationContext>> operations,
            IReadOnlyDictionary<Type, IPipelineContextAsync<MyApplicationContext>> asyncOperations
        )
            :base(operations, asyncOperations)
        {
            Context = new MyApplicationContext();
        }
   }

Step 3: Create Your Operations
------------------------------

Non-Async Operations
^^^^^^^^^^^^^^^^^^^^

* Create a Marker Interface that inherits from ``IPipelineOperation<T>``
* Inherit from ``BaseOperation<T>`` and implement the Marker Interface
* Implement the ``Execute()`` method

.. code-block:: csharp
   :linenos:
   
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces; 
   
   // Marker Interface 
   public interface IMyOperation : IPipelineOperation<MyApplicationContext> { }
   
   public class MyOperation : BaseOperation<MyApplicationContext>, IMyOperation
   {
        public override void Execute(MyApplicationContext context)
        {
            // Test for previous operation's success/failure if necessary
            if(!context.Successful)
                return;
                
            // Implement operation logic here using the context as needed
            
            // Report result
            context.ResultMessages.Add("[MyOperation] Successfully executed my operation");
        }
   }

Async Operations
^^^^^^^^^^^^^^^^

* Create a Marker Interface that inherits from ``IPipelineOperationAsync<T>``
* Inherit from ``BaseOperationAsync<T>`` and implement the Marker Interface
* Implement the ``ExecuteAsync()`` and the ``CompletedTaskCallback()`` methods

.. code-block:: csharp
   :linenos:
   
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces; 
   
   // Marker Interface 
   public interface IMyOperationAsync : IPipelineOperationAsync<MyApplicationContext> { }
   
   public class MyOperationAsync : BaseOperationAsync<MyApplicationContext>, IMyOperationAsync
   {
        private readonly SomeApiClient _someApiClient;
        
        // Constructor with injected repositories or API clients here
        
        // No need to use async/await ... the returned Task is awaited for you
        public override Task ExecuteAsync()
        {
            // Test for previous operation's success/failure if necessary
            if(!Context.Successful)
                return;
                
            // Implement async operation logic here using the Context as needed
            return _someApiClient.GetStuffAsync(Context.SomeId);
        }
        
        public override void CompletedTaskCallback(object task)
        {
            // Good practice to check for proper casting of the task 
            var t = task as Task<IEnumerable<Stuff>>;
            
            if(t == null)
                return;
                
            IEnumerable<Stuff> stuff = t.Result;
            
            Context.ListOfStuff = stuff;
            Context.ResultMessages.Add("[MyOperationAsync] Successfully retrieved stuff");
        }
   }

.. warning::

   If your Operation requires that another Operation be executed before it in the pipeline, then this
   is an Operation-to-Operation dependency and you should add those dependencies to the ``Dependencies``
   collection in the Operation's constructor.
   
   :doc:`See the documentation here <operation-dependencies>`

Step 4: Add Registrations
-------------------------

**Add Service Registrations for Dependency Injection**

* Use the ``AddPipelineCoordinator<TOpMgr, TContext>`` extension method provided with the Pipelines 
  framework.
* ``TOpMgr`` is the type of your Pipeline Coordinator. In this example it is ``MyPipelineCoordinator``
* ``TContext`` is the type of your Pipeline Context. In this example it is ``MyApplicationContext``

Using IServiceCollection
^^^^^^^^^^^^^^^^^^^^^^^^

.. code-block:: csharp
   :linenos:
   
   using Microsoft.Extensions.DependencyInjection;
   using KnightMoves.Pipelines.DependencyInjection;
   
   public class Startup
   {
        ...
        public void ConfigureServices(IServiceCollection services)
        {
            ...
            
            services.AddPipelineCoordinator<MyPipelineCoordinator, MyApplicationContext>
            (
                typeof(Startup).Assembly
            );
            
            ...
        }
   }
   
Using Autofac
^^^^^^^^^^^^^

.. code-block:: csharp 
   :linenos:
   
   using Autofac;
   using KnightMoves.Pipelines.DependencyInjection;
   
   public class Startup
   {
        ...
        
        public LifetimeScope AutofacContainer { get; private set; }
        
        public void ConfigureContainer(ContainerBuilder builder)
        {
            ...
            
            builder.AddPipelineCoordinator<MyPipelineCoordinator, MyApplicationContext>
            (
                typeof(Startup).Assembly
            );
            
            ...
        }
        
        ...
   }

.. tip::

   | Full integration of Autofac is documented here:   
   | https://autofac.readthedocs.io/en/latest/integration/aspnetcore.html

Step 5: Execute and Process
---------------------------

**Use the Operations and process the results**

To use the operations all you have to do is

* Inject your Pipeline Coordinator
* Execute your operations in the order that you choose
* Process the resulting Context as needed 

.. code-block:: csharp 
   :linenos:
   
   using KnightMoves.Pipelines.Interfaces;
   
   public class MyBusinessLogicCoordinator : IBusinessLogic
   {
        private readonly IPipelineCoordinator<MyApplicationContext> _pipelineCoordinator;
        
        public MyBusinessLogicCoordinator(IPipelineCoordinator<MyApplicationContext> pipelineCoordinator)
        {
            _pipelineCoordinator = pipelineCoordinator;
        }
        
        public async Task<IEnumerable<Stuff>> BuildStuffAsync(int data)
        {
            _pipelineCoordinator.Context.Data = data;
            
            // Use Task.Run() if BuildStuff is an Async method 
            await Task.Run(() =>
                
                _pipelineCoordinator
                    .Execute<IMyOperation>()
                    .ExecuteAsync<IMyOperationAsync>()
                
            );
            
            return _pipelineCoordinator.Context.Stuff;
        }
   }














