using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace Serilog.Filters.Microsoft;

/// <summary>
/// Create SourceContextFilter using standard Microsoft configuration values under the root key of "Logging"
/// given a specific IConfigurationRoot instance
/// </summary>
public static class SourceContextFilterFactory
{
    /// <summary>
    /// Create a SourceContextFilter using the global logging level configuration
    /// from the key "Logging:LogLevel"
    /// </summary>
    /// <param name="configurationRoot"></param>
    /// <returns></returns>
    public static SourceContextFilter FromGlobalConfiguration(IConfigurationRoot configurationRoot)
    {
        Dictionary<string, LogEventLevel> sourceContextFilters = new Dictionary<string, LogEventLevel>();
        
        IConfigurationSection sectionLogLevel = configurationRoot.GetSection("Logging:LogLevel");
        return CreateSourceContextFilter(sectionLogLevel, sourceContextFilters);
    }
    
    /// <summary>
    /// Create a SourceContextFilter using the logging level configuration specified by the name of the sink
    /// from the key "Logging:{sinkName}:LogLevel"
    /// </summary>
    /// <param name="configurationRoot"></param>
    /// <param name="sinkName">The name of the sink used to define the log level section in configuration</param>
    /// <returns></returns>
    public static SourceContextFilter FromSinkConfiguration(IConfigurationRoot configurationRoot, string sinkName)
    {
        Dictionary<string, LogEventLevel> sinkSourceContextFilters = new Dictionary<string, LogEventLevel>();
        
        IConfigurationSection sectionLogLevel = configurationRoot.GetSection($"Logging:{sinkName}:LogLevel");
        return CreateSourceContextFilter(sectionLogLevel, sinkSourceContextFilters);
    }

    private static SourceContextFilter CreateSourceContextFilter(IConfigurationSection logLevelConfigurationSection, Dictionary<string, LogEventLevel> sinkSourceContextFilters)
    {
        foreach (IConfigurationSection logLevelKey in logLevelConfigurationSection.GetChildren())
        {
            var sourceContextKey = logLevelKey.Key;
            // Default log level is handled below
            if (sourceContextKey == "Default")
                continue;

            LogEventLevel logEventLevel = GetLogEventLevel(logLevelConfigurationSection, sourceContextKey);
            sinkSourceContextFilters.Add(sourceContextKey, logEventLevel);
        }
        
        LogEventLevel defaultLogLevel = GetLogEventLevel(logLevelConfigurationSection, "Default");
        
        return new SourceContextFilter(sinkSourceContextFilters, defaultLogLevel);
    }

    private static LogEventLevel GetLogEventLevel(IConfigurationSection sinkLogLevel, string logLevelKey)
    {
        LogEventLevel logEventLevel = LogEventLevel.Verbose;

        try
        {
            logEventLevel = sinkLogLevel.GetValue<LogEventLevel>(logLevelKey, LogEventLevel.Verbose);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return logEventLevel;
    }
}