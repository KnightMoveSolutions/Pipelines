`Documentation Home <https://docs.knightmovesolutions.com>`_

==========================
Logging and Error Handling
==========================

One of the benefits of using this framework is that logging and error messages can be consolidated in the :doc:`pipeline-context`
state object, which contains the following two properties.

``IList<string> ResultMessages``

and

``List<Exception> Exceptions``

These cross-cutting concerns can be handled at the end of the execution of the pipeline in order to ensure a centrally located 
place for logging and exception handling.

the :doc:`pipeline-coordinator` does its best to gracefully handle any exceptions thrown or not caught by the Operations you 
create. For :doc:`async-operations` it is more difficult to handle those so it is always best to include try/catch blocks in 
the `ExecuteAsync() and CompletedTaskCallback() <async-operations.html#step-4-implement-operation-logic>`_ methods of any 
:doc:`async-operations`. If you handle exceptions yourself you can add the exception to the `Exceptions` property of the 
application's :doc:`pipeline-context` object and then handle its logging after the pipeline is finished executing. This way,
you don't have to inject and interweave logging code in all of the Operations.

However, there is nothing in the Pipelines framework that forces you to use its logging and exception handling properties.
You are free to implement logging everywhere, use an Aspect-Oriented Programming approach, or not log anything at all. 

If the :doc:`pipeline-coordinator` catches an unhandled exception thrown in any of the Operations executed in the pipeline,
then it will add it to the :doc:`pipeline-context`'s ``Exceptions`` collection and set the ``Successful`` property to 
``false`` for you. 

The :doc:`pipeline-coordinator` provides no logging capabilities whatsoever. You will have to log before the pipeline 
executes, after the pipeline executes, and/or within the Operations themselves if you so choose.


