using System;
using System.Collections.Generic;

namespace KnightMoves.Pipelines.Interfaces
{
    /// <summary>
    /// The state object passed to each operation in a pipeline
    /// </summary>
    public interface IPipelineContext
    {
        /// <summary>
        /// Operations can set this to true/false to notify subsequent operations of sucess or failure.
        /// </summary>
        /// <remarks>
        /// The logic in each operation can decide whether or not to act on the success of previously executed operations.
        /// </remarks>
        bool Successful { get; set; }
        
        /// <summary>
        /// If this is set to true, then no other subsequent operation will be executed
        /// </summary>
        /// <remarks>
        /// Setting this to true effectively kills execution of the rest of the pipeline. Unlike the <see cref="Successful"/>
        /// property where operations still execute if <see cref="Successful"/> is false, no other operations are executed
        /// after <see cref="EndProcessing"/> is set to true.
        /// </remarks>
        bool EndProcessing { get; set; }
        
        /// <summary>
        /// A collection of strings that any operation can publish result information to
        /// </summary>
        IList<string> ResultMessages { get; set; }
        
        /// <summary>
        /// Exceptions that are caught can be gracefully handled and added to this collection to be dealt with
        /// after the pipeline is executed.
        /// </summary>
        /// <remarks>
        /// Exceptions may or may not require setting the <see cref="EndProcessing"/> flag to true and/or <see cref="Successful"/> to false
        /// </remarks>
        IList<Exception> Exceptions { get; set; }
    }
}
