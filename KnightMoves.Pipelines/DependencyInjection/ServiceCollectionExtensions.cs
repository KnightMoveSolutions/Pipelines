using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using KnightMoves.Pipelines.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KnightMoves.Pipelines.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPipelineCoordinator<TOpMgr, TContext>(this IServiceCollection services, Assembly assembly)
            where TOpMgr : IPipelineCoordinator<TContext>
            where TContext : IPipelineContext, new()
        {
            return services.AddPipelineCoordinator<TOpMgr, TContext>(assembly, new List<Type>());
        }

        public static IServiceCollection AddPipelineCoordinator<TOpMgr, TContext>(this IServiceCollection services, Assembly assembly, IEnumerable<Type> forcedImplementations)
            where TOpMgr : IPipelineCoordinator<TContext>
            where TContext : IPipelineContext, new()
        {
            // Pipeline Coordinator
            services.Add(new ServiceDescriptor(typeof(IPipelineCoordinator<TContext>), typeof(TOpMgr), ServiceLifetime.Transient));

            // Operations for IList<> injection
            assembly
                .DefinedTypes
                .Where(x =>
                     x.GetInterfaces().Contains(typeof(IPipelineOperation<TContext>)) &&
                    !x.GetInterfaces().Contains(typeof(IPipelineOperationAsync<TContext>)) &&
                    !x.IsInterface
                )
                .ToList()
                .ForEach(t =>
                    services.Add(new ServiceDescriptor(typeof(IPipelineOperation<TContext>), t, ServiceLifetime.Singleton))
                );

            // Async Operations for IList<> injection
            assembly
                .DefinedTypes
                .Where(x =>
                     x.GetInterfaces().Contains(typeof(IPipelineOperationAsync<TContext>)) &&
                    !x.IsInterface
                )
                .ToList()
                .ForEach(t =>
                    services.Add(new ServiceDescriptor(typeof(IPipelineOperationAsync<TContext>), t, ServiceLifetime.Singleton))
                );

            // Operations for IReadOnlyDictionary<> injection
            services.AddSingleton<IReadOnlyDictionary<Type, IPipelineOperation<TContext>>>(sp =>
            {
                // Grab all operations
                var ops = sp.GetRequiredService<IEnumerable<IPipelineOperation<TContext>>>();

                // Resolve multiple implementations of operations in the ops collection
                var distinctOps = ResolveDuplicateRegistrations<IPipelineOperation<TContext>, TContext>(ops.ToList(), forcedImplementations);

                // Auto-Convert to IDictionary except for the manually registered types
                var opsDict = distinctOps.ToDictionary(x => GetOperationInterfaceType<TContext>(x.GetType()));

                return new ReadOnlyDictionary<Type, IPipelineOperation<TContext>>(opsDict);
            });

            // Async Operations for IReadOnlyDictionary<> injection
            services.AddSingleton<IReadOnlyDictionary<Type, IPipelineOperationAsync<TContext>>>(sp =>
            {
                // Grab all operations
                var ops = sp.GetRequiredService<IEnumerable<IPipelineOperationAsync<TContext>>>();

                // Resolve multiple implementations of operations in the ops collection
                var distinctOps = ResolveDuplicateRegistrations<IPipelineOperationAsync<TContext>, TContext>(ops.ToList(), forcedImplementations);

                // Auto-Convert to IDictionary except for the manually registered types
                var opsDict = distinctOps.ToDictionary(x => GetOperationInterfaceType<TContext>(x.GetType()));

                return new ReadOnlyDictionary<Type, IPipelineOperationAsync<TContext>>(opsDict);
            });

            return services;
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
