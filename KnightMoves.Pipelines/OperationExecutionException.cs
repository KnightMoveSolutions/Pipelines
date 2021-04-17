using System;
using System.Runtime.Serialization;
using KnightMoves.Pipelines.Interfaces;

namespace KnightMoves.Pipelines
{
    /// <summary>
    /// Exception thrown by logic in an <see cref="IPipelineOperation{TContext}"/> object 
    /// or an <see cref="IPipelineOperationAsync{TContext}"/> object.
    /// </summary>
    public class OperationExecutionException : Exception
    {
        public OperationExecutionException() { }

        public OperationExecutionException(string message) : base(message) { }

        public OperationExecutionException(string message, Exception inner) : base(message, inner) { }

        public OperationExecutionException(SerializationInfo info, StreamingContext context): base(info, context) { }
    }
}
