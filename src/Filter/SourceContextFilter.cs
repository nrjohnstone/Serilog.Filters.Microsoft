using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Filters.Microsoft;

/// <summary>
/// Serilog filter that mimics the functionality provided by the Microsoft logging configuration filtering.
/// Filters log events based on SourceContext entries with corresponding log levels.
/// Entries are matched from most specific (longest) to least specific (shortest)
/// </summary>
public class SourceContextFilter : ILogEventFilter
{
    private readonly Dictionary<string, LogEventLevel> _sourceContextFilters;
    private readonly LogEventLevel _defaultLogEventLevel;
    private readonly IOrderedEnumerable<string> _keysInOrder;

    public SourceContextFilter(Dictionary<string, LogEventLevel> sourceContextFilters, LogEventLevel defaultLogEventLevel = LogEventLevel.Verbose)
    {
        _sourceContextFilters = sourceContextFilters ?? throw new ArgumentNullException(nameof(sourceContextFilters));
        
        // Ensure that longer (ie more specific) filters are checked first
        _keysInOrder = _sourceContextFilters.Keys.OrderByDescending(x => x);
        
        _defaultLogEventLevel = defaultLogEventLevel;
    }

    public bool IsEnabled(LogEvent logEvent)
    {
        if (_sourceContextFilters.Count > 0 && logEvent.Properties.ContainsKey("SourceContext"))
        {
            string? sourceContext = string.Empty;
            
            if (logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? propertyValue))
            {
                if (propertyValue is ScalarValue scalar)
                {
                    sourceContext = scalar.Value?.ToString();
                }
            }

            if (!string.IsNullOrEmpty(sourceContext))
            {
                foreach (var sourceContextFilter in _keysInOrder)
                {
                    // If the sourceContextFilter does not match the sourceContext exactly, add a "." so that it must match the full category name
                    if (sourceContext.Equals(sourceContextFilter) || MatchesLeadingCategories(sourceContext, sourceContextFilter))
                    {
                        if (logEvent.Level >= _sourceContextFilters[sourceContextFilter])
                        {
                            return true;
                        }
                    
                        return false;
                    }
                }
            }
        }

        // Use default log level
        return logEvent.Level >= _defaultLogEventLevel;

    }

    private static bool MatchesLeadingCategories(string sourceContext, string sourceContextFilter)
    {
        return sourceContext.StartsWith($"{sourceContextFilter}.");
    }
}