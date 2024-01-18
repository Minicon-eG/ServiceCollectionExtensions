using Microsoft.Extensions.Logging;

namespace Minicon.ServiceCollectionExtensions.LoggingInterceptors;

public class LoggingInterceptorOption
{
	public LogLevel LogLevel { get; set; } = (LogLevel) 2;

	public string NamespaceFilter { get; set; } = "*";

	public string MethodFilter { get; set; } = "*";

	public string TypeFilter { get; set; } = "*";

	public bool LogResult { get; set; }

	public bool LogArguments { get; set; }

	public bool LogTime { get; set; }
}