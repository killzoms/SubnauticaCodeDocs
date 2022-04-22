using System;

namespace FMODUnity
{
    public class EventNotFoundException : Exception
    {
        public Guid Guid;

        public string Path;

        public EventNotFoundException(string path)
            : base("[FMOD] Event not found '" + path + "'")
        {
            Path = path;
        }

        public EventNotFoundException(Guid guid)
            : base(("[FMOD] Event not found " + guid.ToString("b")) ?? "")
        {
            Guid = guid;
        }
    }
}
