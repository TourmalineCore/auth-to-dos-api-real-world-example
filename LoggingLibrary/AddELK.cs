using System;
using System.IO;
using System.Linq;

using Elastic.CommonSchema.Serilog;

using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace LoggingLibrary;

public static class ElkLogger
{
    public static void SetupLogger(
        string elasticSearchUri,
        string elasticSearchLogin,
        string elasticSearchPassword
    )
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console(new UtcEcsTextFormatter())
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticSearchUri))
            {
                AutoRegisterTemplate = true,
                ModifyConnectionSettings = x => x.BasicAuthentication(elasticSearchLogin, elasticSearchPassword),
                IndexFormat = "LogsA-LOGS-{0:yyyy.MM.dd}",
                CustomFormatter = new UtcEcsTextFormatter()
            })
            .WriteTo.TestCorrelator()
            .CreateLogger();
    }

    public static void CloseLogger()
    {
        Log.CloseAndFlush();
    }
}

public class UtcEcsTextFormatter : EcsTextFormatter
{
    public override void Format(
        LogEvent logEvent,
        TextWriter output
    )
    {
        var properties = logEvent
        .Properties
        .Select(kvp => new LogEventProperty(kvp.Key, kvp.Value))
        .ToArray();

        var utcLogEvent = new LogEvent(
            logEvent.Timestamp.ToUniversalTime(),
            logEvent.Level,
            logEvent.Exception,
            logEvent.MessageTemplate,
            properties);

        using (var sw = new StringWriter())
        {
            base.Format(utcLogEvent, sw);
            var formattedMessage = sw.ToString();
            var cleanedMessage = formattedMessage.Replace("+00:00", "");
            output.Write(cleanedMessage);
        }
    }
}