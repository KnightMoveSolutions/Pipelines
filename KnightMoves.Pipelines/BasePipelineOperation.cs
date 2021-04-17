using System;
using System.Collections.Generic;
using KnightMoves.Pipelines.Interfaces;

namespace KnightMoves.Pipelines
{
    public abstract class BasePipelineOperation<TContext> : IPipelineOperation<TContext> where TContext : IPipelineContext
    {
        public List<Type> Dependencies { get; } = new List<Type>();

        public abstract void Execute(TContext context);
    }
}
