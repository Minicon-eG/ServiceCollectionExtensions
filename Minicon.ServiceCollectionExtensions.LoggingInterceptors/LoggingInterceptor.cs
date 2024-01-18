using System.Runtime.CompilerServices;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Minicon.ServiceCollectionExtensions.LoggingInterceptors;

public sealed class LoggingInterceptor<T> : IInterceptor
{
	private readonly ILogger<T> _logger;
	private readonly IOptionsMonitor<LoggingInterceptorOptions> _options;
	private readonly IResultLogFactory _resultLogFactory;
	private LoggingInterceptorOptions? _currentOptions;

	public LoggingInterceptor(
		ILogger<T> logger,
		IOptionsMonitor<LoggingInterceptorOptions> options,
		IResultLogFactory resultLogFactory)
	{
		_logger = logger;
		_options = options;
		_resultLogFactory = resultLogFactory;
		_options.OnChange(OnOptionsChange);
	}

	public void Intercept(IInvocation invocation)
	{
		if (_currentOptions == null)
		{
			_currentOptions = _options.CurrentValue;
		}

		if (!LoggingInterceptor<T>.TryHoleLoggingOptions(invocation, _currentOptions, out LoggingInterceptorOption options))
		{
			invocation.Proceed();
			return;
		}

		using (_logger.BeginScope(invocation.Method.Name))
		{
			if (options.LogArguments)
			{
				LogParameter(invocation.Arguments, options);
			}

			DateTime now = DateTime.Now;
			invocation.Proceed();
			if (options.LogTime)
			{
				_logger.LogInformation("{MethodName} took {TotalMilliseconds}ms",
					invocation.Method.Name, (DateTime.Now - now).TotalMilliseconds);
			}

			if (!options.LogResult || invocation.GetConcreteMethod().ReturnType.Name == "Void")
			{
				return;
			}

			LogReturnValue(invocation.ReturnValue, options);
		}
	}

	private void OnOptionsChange(LoggingInterceptorOptions newOptions, string arg2)
	{
		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformation("Loaded new logging options");
		}

		_currentOptions = newOptions;
	}

	private void LogParameter(object?[] arguments, LoggingInterceptorOption option)
	{
		if (arguments.Length == 0 || !_logger.IsEnabled(option.LogLevel))
		{
			return;
		}

		_logger.Log(option.LogLevel, "Parameters: {Parameters}",
			ParametersInfo(arguments));
	}

	private static string ParametersInfo(object?[] parameters)
	{
		ArgumentNullException.ThrowIfNull(parameters);

		return string.Join(", ", ((IEnumerable<object>)parameters).Select(
			(Func<object, string>)(parameter =>
			{
				var interpolatedStringHandler =
					new DefaultInterpolatedStringHandler(3, 2);
				interpolatedStringHandler.AppendFormatted(parameter?.GetType().Name ?? "unbekannt");
				interpolatedStringHandler.AppendLiteral(":\"");
				interpolatedStringHandler.AppendFormatted(parameter ?? "null");
				interpolatedStringHandler.AppendLiteral("\"");
				return interpolatedStringHandler.ToStringAndClear();
			})));
	}

	private void LogReturnValue(object returnValue, LoggingInterceptorOption option)
	{
		if (!_logger.IsEnabled(option.LogLevel))
		{
			return;
		}

		_logger.Log(option.LogLevel, "Result: {ReturnValue}",
			_resultLogFactory.Create(returnValue));
	}

	private static bool TryHoleLoggingOptions(
		IInvocation invocation,
		LoggingInterceptorOptions currentOptions,
		out LoggingInterceptorOption options)
	{
		options = new LoggingInterceptorOption();
		foreach (LoggingInterceptorOption currentOption in currentOptions)
		{
			if (!new SimpleFilter(currentOption.MethodFilter).IsMatch(invocation.Method.Name) ||
			    !new SimpleFilter(currentOption.NamespaceFilter).IsMatch(
				    invocation.Method.DeclaringType?.Namespace ?? "") ||
			    !new SimpleFilter(currentOption.TypeFilter).IsMatch(() =>
				    invocation.InvocationTarget.GetType().Name))
			{
				continue;
			}

			options = currentOption;
			return true;
		}

		return false;
	}

	public interface IResultLogFactory
	{
		string? Create(object? input);
	}

	public sealed class ResultLogFactory : IResultLogFactory
	{
		public string Create(object? input)
		{
			if (input == null || !typeof(IEnumerable<object>).IsAssignableFrom(input?.GetType()))
			{
				return input?.ToString() ?? "";
			}

			return string.Join("", "'", string.Join("', '",
				((IEnumerable<object>)input).Select(
					(Func<object, string>)(subItem => Create(subItem)))), "'");
		}
	}
}
