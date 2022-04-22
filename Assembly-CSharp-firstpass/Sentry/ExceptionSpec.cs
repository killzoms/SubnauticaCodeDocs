using System;
using System.Collections.Generic;

namespace Sentry
{
    [Serializable]
    public class ExceptionSpec
    {
        public string type;

        public string value;

        public StackTraceContainer stacktrace;

        public ExceptionSpec(string type, string value, List<StackTraceSpec> stacktrace)
        {
            this.type = type;
            this.value = value;
            this.stacktrace = new StackTraceContainer(stacktrace);
        }
    }
}
