using System.Collections.Generic;

namespace Sentry
{
    public class SentryExceptionEvent : SentryEvent
    {
        public ExceptionContainer exception;

        public SentryExceptionEvent(string exceptionType, string exceptionValue, List<Breadcrumb> breadcrumbs, List<StackTraceSpec> stackTrace)
            : base(exceptionType, breadcrumbs)
        {
            exception = new ExceptionContainer(new List<ExceptionSpec>
            {
                new ExceptionSpec(exceptionType, exceptionValue, stackTrace)
            });
        }
    }
}
