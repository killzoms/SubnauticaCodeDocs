using UnityEngine;

namespace AssemblyCSharp
{
    public class BaseWaterPlane : MonoBehaviour
    {
        private static float visThresh = 0.0001f;

        public bool waterPlaneStayVisible;

        [AssertNotNull]
        public Transform waterPlane;

        [AssertNotNull]
        public Renderer waterRender;

        [AssertNotNull(AssertNotNullAttribute.Options.AllowEmptyCollection)]
        public Renderer[] fogRenderers;

        [AssertNotNull(AssertNotNullAttribute.Options.AllowEmptyCollection)]
        public Renderer[] waterOnWallRenderers;

        private float _leakAmount;

        [HideInInspector]
        public Transform hostTrans;

        [HideInInspector]
        public bool isGhost;

        public float waterlevel;

        private bool _waterVisible;

        private Transform[] children;

        private Vector3 extentsPos;

        private Vector3 extentsSize;

        private Renderer clipRender;

        public float leakAmount
        {
            get
            {
                return _leakAmount;
            }
            set
            {
                _leakAmount = value;
                _waterVisible = !isGhost && (_leakAmount > 0f || waterPlaneStayVisible);
                if (waterPlaneStayVisible && base.transform.position.y > Ocean.main.GetOceanLevel())
                {
                    _waterVisible = false;
                }
                UpdateChildrenActive();
                if (_waterVisible)
                {
                    UpdateMaterial();
                }
            }
        }

        public void UpdateChildrenActive()
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.SetActive(_waterVisible);
            }
        }

        public void OnPlayerEntered(Player p)
        {
            waterRender.enabled = true;
            if (clipRender != null)
            {
                clipRender.enabled = true;
            }
        }

        public void OnPlayerExited(Player p)
        {
            waterRender.enabled = false;
            if (clipRender != null)
            {
                clipRender.enabled = false;
            }
        }

        private void Awake()
        {
            BoxCollider component = GetComponent<BoxCollider>();
            extentsPos = component.center;
            extentsSize = component.size;
            Object.Destroy(component);
            children = base.gameObject.GetComponentsInChildren<Transform>();
            leakAmount = 0f;
            UpdateChildrenActive();
        }

        private void OnAddedToBase(Base baseComp)
        {
            isGhost = !(baseComp != null) || baseComp.isGhost;
            isGhost = isGhost || GetComponentInParent<ConstructableBase>() != null;
            leakAmount = 0f;
            UpdateChildrenActive();
        }

        private float GetClipValue(Vector3 newPos)
        {
            float num = Mathf.Abs(extentsSize.y) * 0.5f;
            _ = extentsPos;
            float num2 = num + extentsPos.y;
            float num3 = newPos.y / num2;
            return 1f - Mathf.Sqrt(1f - num3 * num3);
        }

        public void UpdateMaterial()
        {
            Vector3 localPosition = waterPlane.transform.localPosition;
            _ = base.transform.forward;
            if (hostTrans != null)
            {
                localPosition.y = waterlevel - hostTrans.position.y;
                waterPlane.transform.localPosition = localPosition;
                float clipValue = GetClipValue(localPosition);
                waterRender.material.SetFloat(ShaderPropertyID._ClipedValue, clipValue);
                for (int i = 0; i < fogRenderers.Length; i++)
                {
                    fogRenderers[i].material.SetFloat(ShaderPropertyID._LocalFloodLevel, leakAmount);
                }
                for (int j = 0; j < waterOnWallRenderers.Length; j++)
                {
                    waterOnWallRenderers[j].material.SetFloat(ShaderPropertyID._LocalFloodLevel, waterlevel);
                }
            }
        }
    }
}
