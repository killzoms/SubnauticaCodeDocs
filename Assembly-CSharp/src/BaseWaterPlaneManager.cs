using UnityEngine;

namespace AssemblyCSharp
{
    public class BaseWaterPlaneManager : MonoBehaviour
    {
        private BaseWaterPlane[] waterPlanes;

        private VFXSubLeakPoint[] leakPoints;

        private float _leakAmount;

        private float waterlevel;

        public float leakAmount
        {
            get
            {
                return _leakAmount;
            }
            set
            {
                if (_leakAmount != value)
                {
                    _leakAmount = value;
                    for (int i = 0; i < waterPlanes.Length; i++)
                    {
                        waterPlanes[i].leakAmount = _leakAmount;
                    }
                }
            }
        }

        private void UpdateWaterLevel()
        {
            float num = (0f - Base.cellSize.y) * 0.5f;
            float num2 = Base.cellSize.y * 0.5f;
            waterlevel = base.transform.position.y + num + (num2 - num) * leakAmount;
        }

        private void Update()
        {
            UpdateWaterLevel();
            if (waterPlanes != null)
            {
                for (int i = 0; i < waterPlanes.Length; i++)
                {
                    waterPlanes[i].waterlevel = waterlevel;
                }
            }
            for (int j = 0; j < leakPoints.Length; j++)
            {
                leakPoints[j].waterlevel = waterlevel;
            }
        }

        public void SetHost(Transform host)
        {
            UpdateWaterLevel();
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null)
            {
                Int3 cell = componentInParent.WorldToGrid(base.transform.position);
                Transform cellObject = componentInParent.GetCellObject(cell);
                waterPlanes = cellObject.GetComponentsInChildren<BaseWaterPlane>(includeInactive: true);
            }
            for (int i = 0; i < waterPlanes.Length; i++)
            {
                waterPlanes[i].hostTrans = host;
                waterPlanes[i].waterlevel = waterlevel;
                waterPlanes[i].leakAmount = leakAmount;
            }
            leakPoints = GetComponentsInChildren<VFXSubLeakPoint>(includeInactive: true);
        }
    }
}
