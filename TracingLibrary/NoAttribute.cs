using System;

using Microsoft.AspNetCore.Mvc.Filters;

namespace TracingLibrary;

public class NoTracingAttribute : Attribute, IFilterMetadata
{
}