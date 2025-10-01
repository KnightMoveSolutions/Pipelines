using System;
using System.Collections.Generic;

namespace KnightMoves.Pipelines.DependencyInjection;

public class OperationConfig
{
    public List<Type> ForcedImplementations { get; set; } = new();
}
