using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class CoffeeMachineLegacy : MonoBehaviour, IProtoEventListener
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version;

        [AssertNotNull]
        public GameObject coffeeMachinePrefab;

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            if (version < 1 && GetComponent<Pickupable>() == null)
            {
                version = 1;
            }
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            if (version >= 1)
            {
                return;
            }
            if (base.gameObject.GetComponentInParent<SubRoot>() != null)
            {
                global::UnityEngine.Object.Instantiate(coffeeMachinePrefab, base.transform.localPosition, base.transform.localRotation).transform.SetParent(base.transform.parent, worldPositionStays: false);
                global::UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            Pickupable component = base.gameObject.GetComponent<Pickupable>();
            if (!(component != null) || !component.attached)
            {
                global::UnityEngine.Object.Destroy(component);
                version = 1;
            }
        }
    }
}
