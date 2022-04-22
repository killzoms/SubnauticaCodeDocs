using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class AmbientSettings : MonoBehaviour
    {
        [ProtoMember(1)]
        public Color ambientLight = new Color(52f / 255f, 52f / 255f, 52f / 255f);

        public void Capture()
        {
            ambientLight = RenderSettings.ambientLight;
        }

        public override string ToString()
        {
            return ambientLight.ToString();
        }
    }
}
