using System.Collections.Generic;
using UnityEngine;

namespace UWE
{
    public class Event<P>
    {
        public delegate void HandleFunction(P parms);

        public class Handler
        {
            public HandleFunction function;

            public Object obj;

            public bool Trigger(P parms)
            {
                if (!obj || function == null)
                {
                    return false;
                }
                function(parms);
                return true;
            }
        }

        private HashSet<Handler> handlers;

        private List<Handler> toRemove = new List<Handler>();

        private bool triggering;

        public virtual void Trigger(P parms)
        {
            ProfilingUtils.BeginSample("UWE.Event.Trigger");
            try
            {
                if (handlers == null || triggering)
                {
                    return;
                }
                triggering = true;
                foreach (Handler handler in handlers)
                {
                    if (!handler.Trigger(parms))
                    {
                        toRemove.Add(handler);
                    }
                }
                foreach (Handler item in toRemove)
                {
                    handlers.Remove(item);
                }
                toRemove.Clear();
                triggering = false;
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        public void AddHandler(Object obj, HandleFunction func)
        {
            ProfilingUtils.BeginSample("UWE.Event.AddHandler");
            try
            {
                if (handlers == null)
                {
                    handlers = new HashSet<Handler>();
                }
                Handler handler = new Handler();
                handler.obj = obj;
                handler.function = func;
                handlers.Add(handler);
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        public bool RemoveHandler(Object obj, HandleFunction func = null)
        {
            bool result = false;
            ProfilingUtils.BeginSample("UWE.Event.RemoveHandlers");
            try
            {
                if (handlers == null)
                {
                    return result;
                }
                List<Handler> list = new List<Handler>();
                foreach (Handler handler in handlers)
                {
                    if (handler.obj == obj && (func == null || handler.function == func))
                    {
                        list.Add(handler);
                    }
                }
                foreach (Handler item in list)
                {
                    if (handlers.Remove(item))
                    {
                        result = true;
                    }
                }
                return result;
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        public bool RemoveHandlers(GameObject obj)
        {
            return RemoveHandler(obj);
        }

        public void Clear()
        {
            if (handlers != null)
            {
                handlers.Clear();
            }
        }
    }
}
