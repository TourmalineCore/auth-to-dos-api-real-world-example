using Serilog;
using Serilog.Sinks.TestCorrelator;

using Xunit;

namespace LoggingLibrary.Tests;

public class UtcEcsTextFormatterTests
{
    [Fact]
    public void Logging_ReceiveLogsAndContainsSpanIdAndTraceIdFields()
    {
        var dummyElasticSearchUri = "http://localhost:9200";
        var dummyElasticSearchLogin = "test_login";
        var dummyElasticSearchPassword = "test_password";

        ElkLogger.SetupLogger(
            dummyElasticSearchUri,
            dummyElasticSearchLogin,
            dummyElasticSearchPassword
        );

        using (TestCorrelator.CreateContext())
        {
            Log.Information("Test message");
        }

        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();

        Assert.Single(logEvents);
        Assert.Null(logEvents[0].TraceId);
        Assert.Null(logEvents[0].SpanId);
    }
}