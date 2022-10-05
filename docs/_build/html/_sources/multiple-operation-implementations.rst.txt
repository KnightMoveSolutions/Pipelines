`Documentation Home <https://docs.knightmovesolutions.com>`_

==================================
Multiple Operation Implementations
==================================

All the examples of creating Operations show a single implementation of the Operation's marker interface. In other words, if you 
create Operation ``MyOperation`` with the marker interface ``IMyOperation`` as shown in the code below, then there is only one 
implementation of ``IMyOperation``

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
   
However, what if you want to create another implementation of ``IMyOperation`` to give you the ability to switch
implementations? In other words, suppose you utilized the `Strategy Pattern <https://www.dofactory.com/net/strategy-design-pattern>`_
by creating another implementation of ``IMyOperation``.

.. code-block:: csharp
   :linenos:

   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;

   namespace MyApplication.Operations
   {
       // Marker Interface
       public interface IMyOperation : IPipelineOperation<MyApplicationContext> { }
       
       public class MyAlternativeOperation : BasePipelineOperation<MyApplicationContext>, IMyOperation
       {
           public override void Execute(MyApplicationContext context)
           {
               // Logic goes here
           }
       }           
   }

The Pipelines framework is designed to automatically scan the assemblies, pick up all implementations of 
``IPipelineOperation<MyApplicationContext>``, and then inject them into your :doc:`pipeline-coordinator` 
for you. If you then add your operation to the pipeline with multiple implementations as shown in the code 
below, then it will default to the last one if finds, which is unpredictable.

.. code-block:: csharp

   _pipelineCoordinator
       .Execute<IMyOperation>() // Don't know which IMyOperation implementation will execute
       
In order to resolve this, you will need to create a collection of ``System.Type`` to define the implementations you want 
the :doc:`pipeline-coordinator` to use and feed it into the registration extension method as shown in the code below.

.. code-block:: csharp 
   :linenos:
   
   using KnightMoves.Pipelines.DependencyInjection;
   using Microsoft.Extensions.DependencyInjection;
   
   public class Startup
   {
       ...
       public void ConfigureServices(IServiceCollection services)
       {
           ...
           var implementations = new List<Type>
           {
               typeof(MyAlternativeOperation)
           };
       
           services.AddPipelineCoordinator<MyPipelineCoordinator, MyApplicationContext>
           (
               typeof(Startup).Assembly,
               implementations
           );
           ...
       }
       ...
   }
   
In the code example above, the :doc:`pipeline-coordinator` will use ``MyAlternativeOperation`` in the pipeline when calling
the ``Execute<IMyOperation>()`` method.
