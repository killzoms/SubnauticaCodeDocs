using System;

namespace AssemblyCSharp
{
    [AttributeUsage(AttributeTargets.Field)]
    public class WeakAssetReferenceTypeAttribute : Attribute
    {
        public readonly Type assetType;

        public WeakAssetReferenceTypeAttribute(Type assetType)
        {
            this.assetType = assetType;
        }
    }
}
