namespace Minicon.ServiceCollectionExtensions.LoggingInterceptors;

internal sealed class SimpleFilter
{
	private readonly string _filter;

	public SimpleFilter(string filter) => _filter = filter;

	public bool IsMatch(Func<string> getValue)
	{
		if (_filter.Length == 0)
			return false;
		if (_filter == "*")
			return true;
		string str = getValue();
		if (_filter.EndsWith("*") && _filter.StartsWith("*"))
			return str.Contains(_filter.Substring(1, _filter.Length - 2));
		if (_filter.EndsWith("*"))
			return str.StartsWith(_filter.Substring(0, _filter.Length - 1));
		return _filter.StartsWith("*") ? str.EndsWith(_filter.Substring(1, _filter.Length - 1)) : _filter == str;
	}

	public bool IsMatch(string value) => IsMatch((Func<string>) (() => value));
}