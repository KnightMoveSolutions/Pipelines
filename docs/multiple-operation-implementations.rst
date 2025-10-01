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
       
This can be resolved in one of two ways.

* Provide the selected implementation when during DI registration
* Use a factory registration function that executes logic to determine which implementation to use

Each of these approaches is described below.

------------------------------------------------------------
Providing the Selected Implementation During DI Registration
------------------------------------------------------------

To resolve the selected implementation of your operations when the application is bootstrapped, you will need to create 
a collection of ``System.Type`` to define the implementations you want the :doc:`pipeline-coordinator` to use and feed it 
into the registration extension method as shown in the code below.

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
the ``Execute<IMyOperation>()`` method. This will be the permanent implementation of the operation unless you change the 
``implementations`` collection, rebuild, and redeploy your application. 

If you need to switch implementations at runtime, then you can use the factory approach described below.

------------------------------------------------
Using a Factory Registration Function
------------------------------------------------

If you need to switch implementations at runtime (useful for things like code toggles, feature flags, etc.), then you 
can use a factory function to determine which implementation to use. For this, you will need to create a factory function 
that returns an instance of the `OperationConfig` class as shown in the code below.

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
           services.AddTransient(sp => {

               // Logic here to determine which implementation to use based on 
               // config, environment, sp.GetRequiredService<T>(), etc.
               var useAlternative = true;

               return new OperationConfig
               {
                   ForcedImplementations = useAlternative ? 
                        [ typeof(MyAlternativeOperation) ] : 
                        [ typeof(MyOperation) ]
               };

           });
   
           // OperationConfig will be used to resolve the selected implementations at runtime
           services.AddPipelineCoordinator<MyPipelineCoordinator, MyApplicationContext>(typeof(Startup).Assembly);
           ...
       }
       ...
   }

With the setup above, the factory function will be called each time an instance of the :doc:`pipeline-coordinator` is created.
This will allow you to execute logic to determine which implementation of ``IMyOperation`` to use at runtime.