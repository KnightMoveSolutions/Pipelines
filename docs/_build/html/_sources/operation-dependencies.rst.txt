======================
Operation Dependencies
======================

As your pipeline grows and you add more steps to the logic in the form of :doc:`operations` and :doc:`async-operations` 
you may encounter situations where new business requirements must be implemented. 

Implementing new requirements in the form of new Operations will require you to place the execution of the newly developed
Operation somewhere in the pipeline sequence. Sometimes the implementation of new requirements will require the moving of 
existing Operations up or down in the pipeline order. While this re-ordering of Operations is very easy when using this 
framework, it gives rise to the following errors. 

- Moving an existing Operation up in the pipeline may cause you to place it **above** another Operation that is supposed to 
  be executed before it 
- Moving an existing Operation down in the pipeline may cause you to place it **below** another Operation that needs to be 
  executed before it 
- A new Operation can be added to the pipeline above another Operation that needs to be executed before it 

When one Operation expects another Operation to have been executed before it, then this constitutes an Operation-to-Operation
dependency. Over time, it becomes difficult to ascertain how far up or down in the pipeline is safe to execute an Operation. 
When an Operation expects multiple Operations to have been executed before it, then it gets even more difficult to manage the 
order of Operations in the pipeline.

Operation Dependency Resolution
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

When creating an Operation, you must ascertain what other Operation(s) if any must have been executed before it. If one or more 
Operations must execute before the current Operation, then you should mark those dependencies by adding the Operation marker 
interface types to the Operation's ``Dependencies`` collection in its constructor.

The code below illustrates this.

.. code-block:: csharp
   :linenos:
   
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;
   
   namespace MyApplication.Operations
   {
       // Marker Interface 
       public interface IFilterCustomersOperations : IPipelineOperation<MyApplicationContext> { }
       
       public class FilterCustomersOperation : BasePipelineOperation<MyApplicationContext>, IFilterCustomersOperations
       {
           public override void Execute(MyApplicationContext context)
           {
               // In order for this to work, some other Operation must have been executed 
               // to fetch the Customers and plant them in the Context.Customers collection.
               // If the Customers collection has not been initialized and/or populated, then
               // this code will break
           
               context.EmailCampaignCustomers = context.Customers.Where(c => c.Region == Regions.Midwest);
           }
       }
   }

In order to safely place this Operation somewhere in the pipeline, you can mark its dependencies like so.

.. code-block:: csharp
   :linenos:
   
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;
   
   namespace MyApplication.Operations
   {
       // Marker Interface 
       public interface IFilterCustomersOperations : IPipelineOperation<MyApplicationContext> { }
       
       public class FilterCustomersOperation : BasePipelineOperation<MyApplicationContext>, IFilterCustomersOperations
       {
           public FilterCustomersOperation()
           {
               // Dependency added here 
               Dependencies.Add(typeof(IFetchCustomersOperationAsync));
           }
           
           public override void Execute(MyApplicationContext context)
           {
               context.EmailCampaignCustomers = context.Customers.Where(c => c.Region == Regions.Midwest);
           }
       }
   }
   
Here you are notifying the :doc:`pipeline-coordinator` that this Operation cannot be placed above ``IFetchCustomersOperationAsync`` 
in the pipeline order. If this Operation is moved above ``IFetchCustomersOperationAsync``, then the :doc:`pipeline-coordinator`
will throw an ``OperationDependencyNotExecutedException`` at runtime. 

The unit test for the class that uses the :doc:`pipeline-coordinator` should throw this exception or it will be thrown the very 
first time you run the application. In this way, you are guaranteed the safety of being notified that an Operation-to-Operation 
dependency was not satisfied and you will have to resolve the dependency by moving one of the Operations up or down to ensure 
dependent Operations are executed first.
