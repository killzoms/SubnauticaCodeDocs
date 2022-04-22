using System;
using UnityEngine;

namespace AssemblyCSharp
{
    [Serializable]
    public struct WeakAssetReference
    {
        private static readonly string ResourcesFolderPrefix = "Assets/Resources/";

        [SerializeField]
        private string guid;

        [SerializeField]
        private string path;

        public bool IsValid => !string.IsNullOrEmpty(guid);

        public string Path => path;

        public static bool IsValidAssetPath(string assetPath)
        {
            if (!string.IsNullOrEmpty(assetPath))
            {
                return assetPath.StartsWith(ResourcesFolderPrefix);
            }
            return false;
        }

        public static string AssetPathToResourcePath(string assetPath)
        {
            string text = assetPath.Substring(ResourcesFolderPrefix.Length);
            int num = text.LastIndexOf('.');
            if (num != -1)
            {
                text = text.Substring(0, num);
            }
            return text;
        }

        internal WeakAssetReference(string guid, string path)
        {
            this.guid = guid;
            this.path = path;
        }

        public T Load<T>() where T : global::UnityEngine.Object
        {
            return Resources.Load<T>(Path);
        }
    }
}
