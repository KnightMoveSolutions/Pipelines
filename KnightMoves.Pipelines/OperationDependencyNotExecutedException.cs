using System;
using System.Runtime.Serialization;
using KnightMoves.Pipelines.Interfaces;

namespace KnightMoves.Pipelines
{
    /// <summary>
    /// Exception thrown by implementations of <see cref="IPipelineCoordinator{TContext}"/> when
    /// <see cref="IPipelineOperation{TContext}.Dependencies"/> are NOT met
    /// </summary>
    public class OperationDependencyNotExecutedException : Exception
    {
        public OperationDependencyNotExecutedException() { }

        public OperationDependencyNotExecutedException(string message) : base(message) { }

        public OperationDependencyNotExecutedException(string message, Exception inner) : base(message, inner) { }

        public OperationDependencyNotExecutedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
