using System;
using ProtoBuf;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class ColoredLabel : HandTarget, IHandTarget, IProtoEventListener
    {
        [AssertNotNull]
        public string stringEditLabel = "EditLabel";

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public string text = "";

        [NonSerialized]
        [ProtoMember(3)]
        public int colorIndex;

        [AssertNotNull]
        public uGUI_SignInput signInput;

        private void OnEnable()
        {
            signInput.gameObject.SetActive(value: true);
        }

        private void OnDisable()
        {
            signInput.gameObject.SetActive(value: false);
        }

        public void OnHandHover(GUIHand hand)
        {
            if (base.enabled)
            {
                HandReticle.main.SetInteractText(stringEditLabel);
                HandReticle.main.SetIcon(HandReticle.IconType.Interact);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (base.enabled)
            {
                signInput.Select(lockMovement: true);
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            text = signInput.text;
            colorIndex = signInput.colorIndex;
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            signInput.text = text;
            signInput.colorIndex = colorIndex;
        }
    }
}
