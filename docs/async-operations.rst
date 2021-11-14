================
Async Operations
================

Operations are where the logic of the pipeline reside. Here we will be discussing Async Operations. You should create this type
of Operation to implement logic that merits asynchronous calls such as 

- Long-running calculations
- File System IO
- Network Communication (e.g. REST API calls, RPC, print jobs, etc.)
- Reads/Writes from/to a database
- etc.

.. NOTE::
   Not everything should be an asynchronous operation. If it truly doesn't merit the added complexity of an asynchronous call 
   (e.g. simple data validation) then `create a regular Operation <operations.html>`_ instead.
   
To create a async Operation follow the steps below.

Steps to Create an Operation
----------------------------

**Pre-Requisite**

You must have created the :doc:`pipeline-context` object that your Operation will use as a state object. 
The :doc:`pipeline-coordinator` will take care of setting the state object in the Operation's ``Context`` 
property. Unlike non-Async Operations, where the ``PipelineContext`` is injected into the method call, 
for async Operations the ``PipelineContext`` is planted in the ``Context`` property.

Step 1: Add New Class
^^^^^^^^^^^^^^^^^^^^^

.. code-block:: csharp
   :linenos:
   
   using System;
   using System.Collections.Generic;
   using System.Text;
   
   namespace MyApplication.Operations
   {
       public class MyOperationAsync
       {
       }
   }

.. NOTE::
   It is recommended that you suffix the class with "OperationAsync" as a naming convention or at least "Async" for 
   self-documenting code.

Step 2: Create Marker Interface
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Create a marker interface that inherits from ``IPipelineOperationAsync<TContext>`` and specify the type of the application's 
:doc:`pipeline-context` state object that this Operation will handle as its ``TContext``.

.. code-block:: csharp 
   :linenos:
   
   using using KnightMoves.Pipelines.Interfaces;
   
   namespace MyApplication.Operations
   {
       // Marker Interface 
       public interface IMyOperationAsync : IPipelineOperationAsync<MyApplicationContext> { }
       
       public class MyOperationAsync
       {
       }
   }

Step 3: Inherit and Implement
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Inherit from ``BasePipelineOperationAsync`` and implement the ``IMyOperationAsync`` marker interface.

.. code-block:: csharp 
   :linenos:
   
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;
   
   namespace MyApplication.Operations
   {
       // Marker Interface 
       public interface IMyOperationAsync : IPipelineOperationAsync<MyApplicationContext> { }
       
       public class MyOperationAsync : BasePipelineOperationAsync<MyApplicationContext>, IMyOperationAsync
       {
       }
   }
   
Step 4: Implement Operation Logic
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Implementing an async Operation will require overriding the implementation of the following two methods.

``Task ExecuteAsync()``

and

``void CompletedTaskCallback(object task)``

as shown bleow

.. code-block:: csharp
   :linenos:

   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;

   namespace MyApplication.Operations
   {
       // Marker Interface
       public interface IMyOperationAsync : IPipelineOperationAsync<MyApplicationContext> { }
       
       public class MyOperationAsync : BasePipelineOperationAsync<MyApplicationContext>, IMyOperationAsync
       {
           private readonly ISomeApiClient _someApiClient;
           
           // Constructor
           public MyOperationAsync(ISomeApiClient someApiClient)
           {
               _someApiClient = someApiClient;
           }
           
           // No need to use async/await ... the returned Task is awaited for you 
           public override Task ExecuteAsync()
           {
               // Test for previous operations' success/failure if necessary
               if (!Context.Successful)
                   return Task.FromResult(false);               
               
               // Implement async operation logic here using the context as needed
               return _someApiClient.GetStuffAsync(Context.SomeId);
           }
           
           public override void CompletedTaskCallback(object task)
           {
               // Good practice to check for proper casting of the task 
               var t = task as Task<IEnumerable<Stuff>>;
               
               if (t == null)
                   return;
                   
               IEnumerable<Stuff> stuff = t.Result;
               
               Context.ListOfStuff = stuff;
               Context.ResultMessages.Add("[MyOperationAsync] Successfully retrieved stuff");
           }
       }           
   }
   
.. NOTE::
   For Async Operations, the ``PipelineContext`` is planted in the ``Context`` property of the Operation itself. 
   With `non-Async Operations <operations.html>`_ the ``PipelineContext`` is passed to the ``Execute(TContext context)``
   method.
   
.. WARNING::
   If your Operation requires that another Operation be executed before it in the pipeline, then this is an 
   Operation-to-Operation dependency and you should add those dependencies to the ``Dependencies`` collection
   in the Operation's constructor.
   
   `See the documentation for this here <operation-dependencies.html>`_
   
Using the Pipeline Context
--------------------------

Successful
^^^^^^^^^^

The :doc:`pipeline-context` object contains a boolean property called ``Successful`` documented in the :doc:`pipeline-context` page.
You can examine this property to make a decision on whether or not to do something. 

.. code-block:: csharp
   :linenos:
   
   // removed outer code blocks for brevity
   
       public override Task ExecuteAsync()
       {
           if(!Context.Successful)
           {
               // Do nothing
               return Task.FromResult(false);
           }
           
           // Logic goes here
           return Task.FromResult(true);
       }
       
       public override void CompletedTaskCallback(object task)
       {
           // Maybe something went wrong in the logic here but 
           // it doesn't require terminating the whole pipeline
           Context.Successful = false;
       }

EndProcessing
^^^^^^^^^^^^^

You can cancel the execution of the rest of the pipeline by setting the ``EndProcessing`` property to true. The 
:doc:`pipeline-coordinator` will not execute any Operation in the pipeline after this if it is set to true.

.. code-block:: csharp 
   :linenos:
   
   // removed outer code blocks for brevity
   
       public override Task ExecuteAsync()
       {
           // Logic here resulted in some critical failure so we terminate
           // the execution of all other Operations after this 
           
           Context.EndProcessing = true;
           
           return Task.FromResult(false);
       }
       
       public override void CompletedTaskCallback(object task)
       {
           // Maybe something went wrong in the logic here 
           Context.EndProcessing = true;
       }
       
ResultMessages
^^^^^^^^^^^^^^

You can (and should) report the result of the Operation's execution by putting a message in the ``ResultMessages`` collection. 
It can then be used at the end of the pipeline execution for logging and debugging.

.. code-block:: csharp 
   :linenos:
   
   // removed outer code blocks for brevity
   
       public override Task ExecuteAsync()
       {
           var okay = true;
           
           // Logic goes here and sets okay to false if something went wrong
           
           if(!okay)
           {
               Context.ResultMessages("MyOperationAsync Failed!");
               return Task.FromResult(false);
           }
           
           return Task.FromResult(true);
       }
       
       public override void CompletedTaskCallback(object task)
       {
           // Used the completed task to do stuff here 
           
           // Then we report the result 
           Context.ResultMessages("[MyOperationAsync] Successfully executed!");
       }
       
Later the :doc:`pipeline-context` can be used for logging and debugging.

.. code-block:: csharp
   :linenos:
   
   public static void Main(string[] args)
   {
      // ...
      
      _pipelineCoordinator
          .ExecuteAsync<IMyOperation>()
          .ExecuteAsync<ISaveResults>()
      ;
      
      LogOperationResults(_pipelineCoordinator.Context.ResultMessages);
      
      // ...
      
   }
   
   private static void LogOperationResults(IList<string> results)
   {
       // Log results here 
   }
   
Exceptions
^^^^^^^^^^

If exceptions are caught in the Operation's logic and you want to gracefully handle them in a try/catch block, then you can 
plant the exception in the ``Exceptions`` collection of the :doc:`pipeline-context` for logging and debugging later.

.. code-block:: csharp 
   :linenos:
   
   // removed outer code blocks for brevity
   
       public override Task ExecuteAsync()
       {
           try
           {
               // Some logic goes here
           }
           catch(Exception ex)
           {
               // Doh! Exception!
               Context.Exceptions.Add(ex);
               Context.EndProcessing = true;
               Context.ResultMessages.Add("MyOperationAsync Exception: " + ex.Message);
               return;
           }
           
           // Rest of Logic goes here 
           
           context.ResultMessages.Add("MyOperationAsync Successfully executed!");
       } 

Later the :doc:`pipeline-context` can be used for logging and debugging.

.. code-block:: csharp
   :linenos:
   
   public static void Main(string[] args)
   {
      // ...
      
      _pipelineCoordinator
          .ExecuteAsync<IMyOperation>()
          .ExecuteAsync<ISaveResults>()
      ;
      
      LogExceptions(_pipelineCoordinator.Context.Exceptions);
      
      // ...
      
   }
   
   private static void LogExceptions(IList<string> results)
   {
       // Log results here 
   }
  