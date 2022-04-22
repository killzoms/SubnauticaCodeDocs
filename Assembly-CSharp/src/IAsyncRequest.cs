using System.Collections;

namespace AssemblyCSharp
{
    public interface IAsyncRequest : IEnumerator
    {
        bool isDone { get; }

        float progress { get; }
    }
}
