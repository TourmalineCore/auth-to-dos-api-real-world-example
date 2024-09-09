using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Filters;

using OpenTelemetry.Trace;

namespace TracingLibrary;

public class TracingAttribute : ActionFilterAttribute
{
    private readonly string _projectName;

    public TracingAttribute(string projectName)
    {
        _projectName = projectName;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var noTracing = context
        .ActionDescriptor
        .EndpointMetadata
        .OfType<NoTracingAttribute>()
        .Any();

        if (noTracing)
        {
            await next();
            return;
        }

        if (Activity.Current != null)
        {
            var headers = new AttributeContext(
                activity: Activity.Current,
                context: context.HttpContext
            );

            Activity.Current.AddTag("TraceId", headers.TraceId);
            Activity.Current.AddTag("SpanId", headers.SpanId);
            Activity.Current.AddTag("ParentSpanId", headers.ParentSpanId);
            Activity.Current.AddTag("ProjectName", _projectName);
            Activity.Current.AddTag("SentryTrace", headers.SentryTraceHeader);
        }

        var executedContext = await next();

        if (executedContext.Exception != null && !executedContext.ExceptionHandled)
        {
            if (Activity.Current != null)
            {
                Activity.Current.AddTag("ErrorStackTrace", executedContext.Exception.StackTrace);
                Activity.Current.RecordException(executedContext.Exception);
            }

            throw executedContext.Exception;
        }
    }
}