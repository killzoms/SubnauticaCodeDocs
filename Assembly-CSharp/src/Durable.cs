using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class Durable : MonoBehaviour
    {
        [NonSerialized]
        public int version = 2;

        [NonSerialized]
        public int mUses = -1;

        private void Start()
        {
            global::UnityEngine.Object.Destroy(this);
        }
    }
}
