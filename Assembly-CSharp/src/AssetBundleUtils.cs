using System.IO;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class AssetBundleUtils
    {
        public static string GetStandAloneLoadPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "AssetBundles");
        }

        public static string GetLoadPath(string assetBundleName)
        {
            return Path.Combine(GetStandAloneLoadPath(), assetBundleName);
        }
    }
}
