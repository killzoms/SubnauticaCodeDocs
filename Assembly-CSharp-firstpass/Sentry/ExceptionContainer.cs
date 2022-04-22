using System;
using System.Collections.Generic;

namespace Sentry
{
    [Serializable]
    public class ExceptionContainer
    {
        public List<ExceptionSpec> values;

        public ExceptionContainer(List<ExceptionSpec> arg)
        {
            values = arg;
        }
    }
}
