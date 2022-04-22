using System;
using System.Collections;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [Obsolete]
    [ProtoContract]
    public class RestoreEscapePodStorage : MonoBehaviour, IProtoEventListener
    {
        [NonSerialized]
        [ProtoMember(1, OverwriteList = true)]
        public byte[] serialData;

        private StorageContainer podStorage;

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            serialData = StorageHelper.Save(serializer, podStorage.storageRoot.gameObject);
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
        }

        private IEnumerator Start()
        {
            EscapePod escapePod;
            while ((escapePod = global::UnityEngine.Object.FindObjectOfType<EscapePod>()) == null)
            {
                yield return new WaitForSeconds(1f);
            }
            podStorage = escapePod.GetComponentInChildren<StorageContainer>();
            using (PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy())
            {
                podStorage.ResetContainer();
                StorageHelper.RestoreItems(pooledObject, serialData, podStorage.container);
            }
            global::UnityEngine.Object.Destroy(this);
        }
    }
}
