using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class LEDLight : PlaceTool, IProtoEventListener
    {
        public Animator animator;

        public ToggleLights toggleLights;

        public Rigidbody rigidBody;

        public LargeWorldEntity lwe;

        [NonSerialized]
        [ProtoMember(1)]
        public bool deployed;

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            UpdateState(deployed);
        }

        private void OnExamine()
        {
            UpdateState(deployed: false);
        }

        private void OnDrop()
        {
            UpdateState(deployed: false);
        }

        private void OnReload()
        {
            UpdateState(deployed: false);
        }

        public override void OnPlace()
        {
            UpdateState(deployed: true);
        }

        private void Update()
        {
            if ((bool)usingPlayer)
            {
                animator.SetBool("using_tool", GetUsedToolThisFrame());
            }
        }

        private void UpdateState(bool deployed)
        {
            this.deployed = deployed;
            if (deployed)
            {
                rigidBody.isKinematic = true;
            }
            LargeWorldEntity.CellLevel cellLevel = LargeWorldEntity.CellLevel.Far;
            if (PlatformUtils.isConsolePlatform)
            {
                cellLevel = LargeWorldEntity.CellLevel.Medium;
            }
            lwe.cellLevel = (deployed ? cellLevel : LargeWorldEntity.CellLevel.Near);
        }
    }
}
