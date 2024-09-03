using System;

using Microsoft.AspNetCore.Mvc.Filters;

namespace LoggingLibrary;

public class NoLoggingAttribute : Attribute, IFilterMetadata
{
}