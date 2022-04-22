using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseExplicitFace : MonoBehaviour
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [ProtoMember(2)]
        public Base.Face? face;

        public BaseDeconstructable parent { get; private set; }

        public static BaseExplicitFace MakeFaceDeconstructable(Transform geometry, Base.Face face, BaseDeconstructable parent = null)
        {
            if (geometry == null)
            {
                return null;
            }
            BaseExplicitFace baseExplicitFace = geometry.gameObject.AddComponent<BaseExplicitFace>();
            baseExplicitFace.face = face;
            baseExplicitFace.parent = parent;
            return baseExplicitFace;
        }
    }
}
