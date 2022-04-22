using System.Collections;

namespace AssemblyCSharp
{
    public interface IAssetBundleWrapperCreateRequest : IAsyncRequest, IEnumerator
    {
        IAssetBundleWrapper assetBundle { get; }
    }
}
