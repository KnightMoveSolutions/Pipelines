`Documentation Home <https://docs.knightmovesolutions.com>`_

========
Overview
========

What is it?
-----------

The Pipelines library is a framework for decomposing and executing processes 
(i.e. PipelineOperations) that are highly dependent on each other and which must be 
executed in a particular order.

What problem does it solve?
---------------------------

This framework simplifies the creation of procedural code that is made up of many steps
that involve a combination of reaching out to other APIs, pulling data from a database,
saving data to a database, calculations, validation logic, sorting/grouping, data 
transformations, and crafting of the output format. 

Without a framework like this, inevitably the code becomes a monolith with a mix of 
steps executing the types of logic above, exception handling throughout, logging 
interweaved with the business logic, if/else blocks, async operations, and early 
returns.

This framework makes it easy to separate (i.e. decompose) portions of logic (i.e. steps in the procedure) into their own classes so that each step adheres to the Single
Responsibility Principle and can be independently managed.

To illustrate, this framework allows you to go from this ...

.. code-block:: csharp
   :linenos:
   
   IEnumerable<Customer> targetCustomers;
   try
   {
       targetCustomers = await _customerApi.GetCustomersBySegment(segmentId);
   }
   catch (Exception ex)
   {
       _logger.LogError(ex.Message);
       return null;
   }
   
   if(targetCustomers == null)
   {
       return null;
   }
   
   List<Customer> validCustomers = new List<Customer>();
   
   targetCustomers.ToList().ForEach(c =>
   {
        if(string.IsNullOrEmpty(c.FirstName))
            return;
		   
        if(string.IsNullOrEmpty(c.LastName))
            return;
        
        if(!IsValidEmailAddress(c.Email))
            return;
            
        validCustomers.Add(c);
   };
   
   const int PURCHASE_HISTORY_MONTHS = 3;
   
   List<EmailCampaignTarget> emailCampaignTargets = new List<EmailCampaignTarget>();
   
   validCustomers.ForEach(c =>
   {
        var purchasedProducts = _purchaseHistoryRepo.GetPurchaseHistory(PURCHASE_HISTORY_MONTHS, c.Id);
        
        if(purchasedProducts.Any(p => p.Category == productCategory))
        {
            emailCampaignTargets.Add(new EmailCampaignTarget
            {
                CustomerId = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email
            });
        }
   };
   
   return emailCampaignTargets;
   
... to this ...

.. code-block:: csharp
   :linenos:

   _pipelineCoordinator.Context.SegmentId = segmentId;
   _pipelineCoordinator.Context.ProductCategory = productCategory;
   
   _pipelineCoordinator
        .ExecuteAsync<IGetCustomersBySegmentOperationAsync>()
        .Execute<IValidateCustomersOperation>()
        .ExecuteAsync<IFilterCustomersByPurchaseHistoryOperationAsync>()
        .Execute<IMapCustomersToEmailCampaignOperation>()
   ;
   
   if(!_pipelineCoordinator.Context.Successful)
   {
        var errorMsg = GetErrorMsg(_pipelineCoordinator.Context.Exceptions);
        _logger.LogError(errorMsg);
        return null;
   }
   
   return _pipelineCoordinator.Context.EmailCampaignTargets;
   
You can see above that the monolithic logic has been decomposed into four steps executed by the
``_pipelineCoordinator``. This simplifies the code and promotes the Single Responsibility Principle.
At a glance you can see what is happening where the manager executes the following operations.

* ``IGetCustomersBySegmentOperationAsync``
* ``IValidateCustomersOperation``
* ``IFilterCustomersByPurchaseHistoryOperationAsync``
* ``IMapCustomersToEmailCampaignOperation``

Non-Async operations have the suffix "Operation" and Async Operations have the suffix "OperationAsync".
This is just a (recommended) naming convention that also makes the code self-documenting.

When do I use it?
-----------------

Anytime you have code that coordinates many steps of logic that are dependent on a particular sequence
and involves a mix of operations such as fetching data from APIs, reading/writing to a database, validation,
calculations, etc. AND it begins to form a monolithic procedural code block of significant size, then
it is a candidate for decomposing into separate operations and you will need some way to coordinate 
those operations. That is when this framework becomes useful and you may consider using it.

What Next?
----------

If you are familiar with this library and need a reminder on the steps necessary to implement
or if you just want to get crackin' then go to the :doc:`quick-start`

If you want to dig under the hood of the framework then go examine :doc:`how-it-works`
