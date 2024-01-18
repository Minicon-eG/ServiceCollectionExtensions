using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Minicon.ServiceCollectionExtensions.LoggingInterceptors;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInterceptor<TInterface, TInterceptor, TImplementation>(
		this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Scoped)
		where TInterface : class
		where TInterceptor : class, IInterceptor
		where TImplementation : class, TInterface
	{
		services.TryAddSingleton<IProxyGenerator, ProxyGenerator>();
		services.TryAdd(new ServiceDescriptor(typeof (TImplementation), typeof (TImplementation), lifetime));
		services.TryAddTransient<TInterceptor>();
		services.SetupByLifetime<TInterface>((Func<IServiceProvider, TInterface>) (sp => sp.GetRequiredService<IProxyGenerator>().CreateInterfaceProxyWithTarget<TInterface>((TInterface) sp.GetRequiredService<TImplementation>(), (IInterceptor) sp.GetRequiredService<TInterceptor>())), lifetime);
		return services;
	}

	public static IServiceCollection AddInterceptor(
		this IServiceCollection services,
		Type interfaceType,
		Type implementationType,
		Type interceptorType,
		ServiceLifetime lifetime = ServiceLifetime.Scoped)
	{
		services.TryAddSingleton<IProxyGenerator, ProxyGenerator>();
		services.TryAdd(new ServiceDescriptor(implementationType, implementationType, lifetime));
		services.TryAddTransient(interceptorType);
		services.SetupByLifetime<object>((Func<IServiceProvider, object>) (sp => sp.GetRequiredService<IProxyGenerator>().CreateInterfaceProxyWithTarget<object>(sp.GetRequiredService(interfaceType), (IInterceptor) sp.GetRequiredService(interceptorType))), lifetime);
		return services;
	}

	private static IServiceCollection SetupByLifetime<TImplementation>(
		this IServiceCollection services,
		Func<IServiceProvider, TImplementation> setup,
		ServiceLifetime lifetime)
		where TImplementation : class
	{
		IServiceCollection serviceCollection;
		switch (lifetime)
		{
			case ServiceLifetime.Singleton:
				serviceCollection = services.AddSingleton<TImplementation>(setup);
				break;
			case ServiceLifetime.Scoped:
				serviceCollection = services.AddScoped<TImplementation>(setup);
				break;
			case ServiceLifetime.Transient:
				serviceCollection = services.AddTransient<TImplementation>(setup);
				break;
			default:
				serviceCollection = services;
				break;
		}
		return serviceCollection;
	}
}