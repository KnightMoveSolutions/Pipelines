using System;
using System.Collections.Generic;
using KnightMoves.Pipelines.Interfaces;

namespace KnightMoves.Pipelines
{
    /// <summary>
    /// A base State object where members of <see cref="IPipelineContext"/> have been initialized.
    /// </summary>
    /// <remarks>
    /// Application state objects can implement this abstract class in order to satisfy the implementation
    /// required by the <see cref="IPipelineContext"/> interface
    /// </remarks>
    public abstract class PipelineContext : IPipelineContext
    {
        public bool Successful { get; set; } = true;
        public bool EndProcessing { get; set; }
        public IList<string> ResultMessages { get; set; } = new List<string>();
        public IList<Exception> Exceptions { get; set; } = new List<Exception>();
    }
}