`Documentation Home <https://docs.knightmovesolutions.com>`_

==========
Operations
==========

Operations are where the logic of the pipeline reside. Here we will be discussing non-async Operations. 
You should create this type of Operation to implement logic that does not do anything that would merit 
asynchronous execution. 

.. WARNING::
   Don't use a non-async Operation when its logic 
   
   * Executes a long running calculation
   * Uses File System IO
   * Communicates over the network (e.g. REST API calls, RPC, print jobs, etc.)
   * Reads / Writes from/to a database
   
To create a non-async Operation follow the steps below.

Steps to Create an Operation
----------------------------

**Pre-Requisite**

You must have created the :doc:`pipeline-context` object that your Operation will use as a state object. 
The :doc:`pipeline-coordinator` will take care of injecting that state object into the Operation's 
``Execute(TContext)`` method.

Step 1: Add New Class
^^^^^^^^^^^^^^^^^^^^^

.. code-block:: csharp
   :linenos:
   
   using System;
   using System.Collections.Generic;
   using System.Text;
   
   namespace MyApplication.Operations
   {
       public class MyOperation
       {
       }
   }

.. NOTE::
   It is recommended that you suffix the class with "Operation" as a naming convention.
   
Step 2: Create Marker Interface
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Create a marker interface that inherits from ``IPipelineOperation<TContext>`` and specify the type of the application's 
:doc:`pipeline-context` state object that this Operation will handle as its ``TContext``.

.. code-block:: csharp 
   :linenos:
   
   using using KnightMoves.Pipelines.Interfaces;
   
   namespace MyApplication.Operations
   {
       // Marker Interface 
       public interface IMyOperation : IPipelineOperation<MyApplicationContext> { }
       
       public class MyOperation
       {
       }
   }
   
Step 3: Inherit and Implement
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Inherit from ``BasePipelineOperation`` and implement the ``IMyOperation`` marker interface.

.. code-block:: csharp 
   :linenos:
   
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;
   
   namespace MyApplication.Operations
   {
       // Marker Interface 
       public interface IMyOperation : IPipelineOperation<MyApplicationContext> { }
       
       public class MyOperation : BasePipelineOperation<MyApplicationContext>, IMyOperation
       {
       }
   }
   
Step 4: Implement Operation Logic
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Implement the ``IPipelineOperation<TContext>```'s ```Execute(TContext)``` method. 

.. code-block:: csharp
   :linenos:

   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;

   namespace MyApplication.Operations
   {
       // Marker Interface
       public interface IMyOperation : IPipelineOperation<MyApplicationContext> { }
       
       public class MyOperation : BasePipelineOperation<MyApplicationContext>, IMyOperation
       {
           public override void Execute(MyApplicationContext context)
           {
               // Logic goes here
           }
       }           
   }
   
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
   
       public override void Execute(MyApplicationContext context)
       {
           if(!context.Successful)
           {
               // Do nothing
               return;
           }
           
           // Logic goes here
       }
       
EndProcessing
^^^^^^^^^^^^^

You can cancel the execution of the rest of the pipeline by setting the ``EndProcessing`` property to true. The 
:doc:`pipeline-coordinator` will not execute any Operation in the pipeline if this is set to true.

.. code-block:: csharp 
   :linenos:
   
   // removed outer code blocks for brevity
   
       public override void Execute(MyApplicationContext context)
       {
           // Logic here resulted in some critical failure so we terminate
           // the execution of all other Operations after this 
           
           context.EndProcessing = true;
       }
       
ResultMessages
^^^^^^^^^^^^^^

You can (and should) report the result of the Operation's execution by putting a message in the ``ResultMessages`` collection. 
It can then be used at the end of the pipeline execution for logging and debugging.

.. code-block:: csharp 
   :linenos:
   
   // removed outer code blocks for brevity
   
       public override void Execute(MyApplicationContext context)
       {
           var okay = true;
           
           // Logic goes here and sets okay to false if something went wrong
           
           if(!okay)
           {
               context.ResultMessages.Add("MyOperation Failed!");
               return;
           }
           
           context.ResultMessages.Add("MyOperation Successfully executed!");
       }
       
Later the :doc:`pipeline-context` can be used for logging and debugging.

.. code-block:: csharp
   :linenos:
   
   public static void Main(string[] args)
   {
      // ...
      
      _pipelineCoordinator
          .Execute<IMyOperation>()
          .Execute<ISaveResults>()
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
   
       public override void Execute(MyApplicationContext context)
       {
           try
           {
               // Some logic goes here
           }
           catch(Exception ex)
           {
               // Doh! Exception!
               context.Exceptions.Add(ex);
               context.EndProcessing = true;
               context.ResultMessages.Add("MyOperation Exception: " + ex.Message);
               return;
           }
           
           // Rest of Logic goes here 
           
           context.ResultMessages.Add("MyOperation Successfully executed!");
       } 

Later the :doc:`pipeline-context` can be used for logging and debugging.

.. code-block:: csharp
   :linenos:
   
   public static void Main(string[] args)
   {
      // ...
      
      _pipelineCoordinator
          .Execute<IMyOperation>()
          .Execute<ISaveResults>()
      ;
      
      LogExceptions(_pipelineCoordinator.Context.Exceptions);
      
      // ...
      
   }
   
   private static void LogExceptions(IList<Exception> results)
   {
       // Log results here 
   }
   
 