using System.Diagnostics;

using Microsoft.AspNetCore.Http;

namespace LoggingLibrary;

public class AttributeContext
{
    public AttributeContext(
        Activity activity,
        HttpContext context
    )
    {
        TraceId = activity.TraceId.ToString();
        SpanId = activity.SpanId.ToString();
        ParentSpanId = activity.ParentSpanId.ToString();
        SentryTraceHeader = context.Request.Headers["sentry-trace"].ToString();
    }

    public string TraceId { get; set; }

    public string SpanId { get; set; }

    public string ParentSpanId { get; set; }

    public string SentryTraceHeader { get; set; }
}