using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PingInstance : MonoBehaviour
    {
        public PingType pingType;

        [AssertNotNull]
        public Transform origin;

        public bool displayPingInManager = true;

        public float minDist = 15f;

        public float maxDist = 10f;

        private const int version = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public bool visible = true;

        [NonSerialized]
        [ProtoMember(3)]
        public int colorIndex;

        private string _label;

        private void OnEnable()
        {
            PingManager.Register(this);
        }

        private void OnDisable()
        {
            PingManager.Unregister(this);
        }

        public void SetVisible(bool visible)
        {
            if (this.visible != visible)
            {
                this.visible = visible;
                PingManager.NotifyVisible(this);
            }
        }

        public string GetLabel()
        {
            return _label;
        }

        public void SetLabel(string value)
        {
            if (!(_label == value))
            {
                _label = value;
                PingManager.NotifyRename(this);
            }
        }

        public void SetColor(int index)
        {
            if (index >= PingManager.colorOptions.Length)
            {
                index = 0;
            }
            colorIndex = index;
            PingManager.NotifyColor(this);
        }
    }
}
