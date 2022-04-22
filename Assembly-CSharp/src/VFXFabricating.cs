using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    [ExecuteInEditMode]
    public class VFXFabricating : MonoBehaviour, ICompileTimeCheckable
    {
        public float localMinY;

        public float localMaxY;

        public Vector3 posOffset;

        public Vector3 eulerOffset;

        public float scaleFactor = 1f;

        public float minY => base.transform.position.y + localMinY;

        public float maxY => base.transform.position.y + localMaxY;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Vector3 vector = base.transform.position + posOffset;
            Vector3 vector2 = base.transform.position + posOffset;
            vector = new Vector3(vector.x, minY, vector.z);
            vector2 = new Vector3(vector2.x, maxY, vector2.z);
            Gizmos.DrawLine(vector, vector2);
        }

        public string CompileTimeCheck()
        {
            HashSet<Type> hashSet = new HashSet<Type>
            {
                typeof(Transform),
                typeof(VFXFabricating),
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(SkinnedMeshRenderer),
                typeof(SkyApplier),
                typeof(LODGroup),
                typeof(Animator)
            };
            List<Component> list = new List<Component>();
            StringBuilder stringBuilder = null;
            GetComponentsInChildren(includeInactive: true, list);
            for (int i = 0; i < list.Count; i++)
            {
                Component component = list[i];
                if (!(component != null))
                {
                    continue;
                }
                Type type = component.GetType();
                if (!hashSet.Contains(type))
                {
                    if (!Targeting.GetRoot(base.gameObject, out var _, out var gameObject))
                    {
                        gameObject = base.gameObject;
                    }
                    if (stringBuilder == null)
                    {
                        stringBuilder = new StringBuilder();
                        stringBuilder.AppendFormat("GameObject (and it's children) with VFXFabricating script will be instantiated as a ghost model for fabrication effect, so it should only contain visual stuff. The following components doesn't meet this requirement:\n");
                    }
                    stringBuilder.AppendFormat(" - '{0}' component on '{1}' gameObject\n", type.Name, gameObject.name);
                }
            }
            if (stringBuilder != null)
            {
                stringBuilder.AppendFormat("Move them out of gameObject with VFXFabricating component hierarchy. Use FollowTransform component on your gameObject if you need to attach it to a specific transform in this hierarchy.\n");
                return stringBuilder.ToString();
            }
            return null;
        }
    }
}
