using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Autofac;
using KnightMoves.Pipelines.Interfaces;

namespace KnightMoves.Pipelines.DependencyInjection
{
    public static class AutofacBuilderExtensions
    {
        public static ContainerBuilder AddPipelineCoordinator<TOpMgr, TContext>(this ContainerBuilder builder, Assembly assembly)
            where TOpMgr : IPipelineCoordinator<TContext>
            where TContext : IPipelineContext, new()
        {
            return builder.AddPipelineCoordinator<TOpMgr, TContext>(assembly, new List<Type>());
        }

        public static ContainerBuilder AddPipelineCoordinator<TOpMgr, TContext>(this ContainerBuilder builder, Assembly assembly, IEnumerable<Type> forcedImplementations)
            where TOpMgr : IPipelineCoordinator<TContext>
            where TContext : IPipelineContext
        {
            // Pipeline Coordinator
            builder.RegisterType<TOpMgr>()
                   .As<IPipelineCoordinator<TContext>>()
                   .InstancePerDependency();

            // Operations for IList<> injection
            builder.RegisterAssemblyTypes(assembly)
                   .Where
                   (t =>
                        t.IsAssignableTo<IPipelineOperation<TContext>>() &&
                       !t.IsAssignableTo<IPipelineOperationAsync<TContext>>()
                   )
                   .AsImplementedInterfaces()
                   .SingleInstance();

            // Async Operations for IList<> injection
            builder.RegisterAssemblyTypes(assembly)
                   .Where(t => t.IsAssignableTo<IPipelineOperationAsync<TContext>>())
                   .AsImplementedInterfaces()
                   .SingleInstance();

            // Operations for IReadOnlyDictionary<> injection
            builder.Register<IReadOnlyDictionary<Type, IPipelineOperation<TContext>>>(context =>
            {
                // Grab all operations
                var ops = context.Resolve<IList<IPipelineOperation<TContext>>>();

                // Resolve multiple implementations of operations in the ops collection
                var distinctOps = ResolveDuplicateRegistrations<IPipelineOperation<TContext>, TContext>(ops, forcedImplementations);

                // Auto-Convert to IDictionary except for the manually registered types
                var opsDict = distinctOps.ToDictionary(x => GetOperationInterfaceType<TContext>(x.GetType()));

                return new ReadOnlyDictionary<Type, IPipelineOperation<TContext>>(opsDict);
            });

            // Async Operations for IReadOnlyDictionary<> injection
            builder.Register<IReadOnlyDictionary<Type, IPipelineOperationAsync<TContext>>>(context =>
            {
                // Grab all operations
                var ops = context.Resolve<IList<IPipelineOperationAsync<TContext>>>();

                // Resolve multiple implementations of operations in the ops collection
                var distinctOps = ResolveDuplicateRegistrations<IPipelineOperationAsync<TContext>, TContext>(ops, forcedImplementations);

                // Auto-Convert to IDictionary except for the manually registered types
                var opsDict = distinctOps.ToDictionary(x => GetOperationInterfaceType<TContext>(x.GetType()));

                return new ReadOnlyDictionary<Type, IPipelineOperationAsync<TContext>>(opsDict);
            });

            return builder;
        }

        private static List<TOp> ResolveDuplicateRegistrations<TOp, TContext>(IList<TOp> ops, IEnumerable<Type> forcedImplementations) where TContext : IPipelineContext
        {
            // RESOLVE DUPLICATE IMPLEMENTATIONS
            // We have to select one implementation when more than two exists
            var distinctOps = new List<TOp>();

            ops
                .Select(o => (interfaceType: GetOperationInterfaceType<TContext>(o.GetType()), implementationType: ops.GetType(), implementation: o))
                .ToList()
                .ForEach(x =>
                {
                    // Do we already have an object in distinctOps that implements x.interface?
                    var existing = distinctOps.SingleOrDefault(t => GetOperationInterfaceType<TContext>(t.GetType()) == x.interfaceType);

                    // If so, we are on the 2nd or Nth implementation so resolve
                    if (existing != null)
                    {
                        // RESOLUTION
                        // If NO selections to resolve multiple implementations have been provided
                        // OR it is NOT the implementation that has been selected
                        // THEN remove the existing one
                        // either the last one in the list or the explicitly selected implementation wins
                        if (!forcedImplementations.Any() || !forcedImplementations.Contains(existing.GetType()))
                        {
                            distinctOps.Remove(existing);
                            distinctOps.Add(x.implementation);
                        }

                        return;
                    }

                    distinctOps.Add(x.implementation);
                });

            return distinctOps;
        }

        private static Type GetOperationInterfaceType<TContext>(Type implementation) where TContext : IPipelineContext
        {
            return implementation.GetInterfaces()
                                 .Where(i => i != typeof(IPipelineOperation<TContext>) && i != typeof(IPipelineOperationAsync<TContext>))
                                 .FirstOrDefault();
        }
    }
}
