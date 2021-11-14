================
Pipeline Context
================

The Pipeline Context is the state object that is passed to each Operation in the Pipeline. The logic of each Operation 
can then use the context state object to read data in it and write data to it in the course of its processing. 

Operations may expect data to be planted in the context state object by other Operations executed before them. They may
also plant data in the context state object to be used by other Operations executed after them in the pipeline.

The Pipeline Context  is the defining characteristic of the pipeline. In essense, the ``TContext`` generic type on the 
:doc:`pipeline-coordinator`, :doc:`operations`, and :doc:`async-operations` basically means that they are saying

  I am designed to work with the data in this type of ``PipelineContext``
  
An application can define more than one ``PipelineContext`` if there are different places in the application where the 
Pipelines framework would be useful. Desktop applications come to mind. In this case each ``PipelineContext`` would be 
used by their own :doc:`pipeline-coordinator`, :doc:`operations`, and :doc:`async-operations` designed to work with 
their respective ``PipelineContext`` state objects. 

IPipelineContext
----------------

The application's ``PipelineContext`` state object must implement the ``IPipelineContext`` interface in ``KnightMoves.Pipelines.Interfaces``
and looks like this. 

.. code-block:: csharp
   :linenos:
   
   public interface IPipelineContext
   {
       bool Successful { get; set; }
       bool EndProcessing { get; set; }
       IList<string> ResultMessages { get; set; }
       IList<Exception> Exceptions { get; set; }
   }
   
A base implementation of ``IOperationContext`` has been provided as part of the framework for convenience and documented in 
the next section below. 

Base PipelineContext Model Object
-------------------------------------

The Pipelines framework provides a base implementation of the ``IPipelineContext`` interface that your application's ``PipelineContext``
can inherit from for convenience and it looks like this. 

.. code-block:: csharp
   :linenos:
   
   public abstrace class PipelineContext : IPipelineContext
   {
       public bool Successful { get; set; } = true;
       public bool EndProcessing { get; set; }
       public IList<string> ResultMessages { get; set; } = new List<string>();
       public List<Exception> Exceptions { get; set; } = new List<Exception>();
   }

Creating Your PipelineContext
-----------------------------

With the base implementation provided above you can create your own context object very easily. 

.. code-block:: csharp
   :linenos:
   using KnightMoves.Pipelines;
   using KnightMoves.Pipelines.Interfaces;
   
   public class MyApplicationContext : PipelineContext, IPipelineContext
   {
       // Add application-specific properties here such as the examples below
       
       public IEnumerable<Customer> Customers { get; set; }
       public IEnumerable<Customer> EmailCampaignRecipients { get; set; }
       
       // ... etc.
   }
   
.. NOTE::

   It might seem redundant to add the IPipelineContext interface to the class above but this is necessary
   for dependency resolution and injection by the IoC container.
   
Now you can create Operations that use this context such as 

``IFetchCustomerOperationAsync``

``IFiltercustomersForEmailCampaignOperation``

Follow the instructions in the :doc:`quick-start`, the :doc:`operations` page, or the :doc:`async-operations` page 
to create your Operations.