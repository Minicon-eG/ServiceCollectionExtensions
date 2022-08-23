using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Minicon.ServiceCollectionExtensions.Tests;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddMockObject<T>(
		this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Scoped
	) where T : class
	{
		services.Add(
			new ServiceDescriptor(typeof(T), _ => new Mock<T>().Object, lifetime)
		);

		return services;
	}
}