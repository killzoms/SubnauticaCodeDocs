using UnityEngine;

namespace AssemblyCSharp
{
    [ExecuteInEditMode]
    public class TestEditorTranslate : MonoBehaviour
    {
        private const float blockSize = 10f;

        private Int3 block;

        private void Update()
        {
            Int3 @int = Int3.Floor(base.transform.position / 10f);
            if (@int != block)
            {
                GameObject gameObject = new GameObject("root " + @int);
                gameObject.transform.position = @int.ToVector3() * 10f;
                Vector3 position = base.transform.position;
                Vector3 localPosition = base.transform.localPosition;
                Transform parent = base.transform.parent;
                base.transform.parent = gameObject.transform;
                Vector3 position2 = base.transform.position;
                Vector3 localPosition2 = base.transform.localPosition;
                Debug.Log(string.Concat("moved from ", position, " to ", position2, " (local ", localPosition, " to ", localPosition2, ")"), this);
                if ((bool)parent)
                {
                    Object.DestroyImmediate(parent.gameObject);
                }
            }
        }
    }
}
