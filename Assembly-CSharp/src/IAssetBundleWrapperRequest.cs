using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public interface IAssetBundleWrapperRequest : IAsyncRequest, IEnumerator
    {
        Object asset { get; }
    }
}
