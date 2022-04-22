using UnityEngine;

namespace AssemblyCSharp
{
    public class WaterParkPiece : MonoBehaviour, IBaseModuleGeometry, IObstacle
    {
        public GameObject floorBottom;

        public GameObject floorMiddle;

        public GameObject ceilingTop;

        public GameObject ceilingGlass;

        public GameObject ceilingMiddle;

        public GameObject bubblesFX;

        private WaterPark waterParkModule;

        public WaterParkPiece lowerPiece;

        private Base.Face _geometryFace;

        public Base.Face geometryFace
        {
            get
            {
                return _geometryFace;
            }
            set
            {
                _geometryFace = value;
            }
        }

        public void ShowBubbles()
        {
            bubblesFX.SetActive(value: true);
        }

        public void ShowFloor()
        {
            floorBottom.SetActive(value: true);
            floorMiddle.SetActive(value: false);
        }

        public void ShowGlassCeiling()
        {
            ceilingTop.SetActive(value: false);
            ceilingGlass.SetActive(value: true);
            ceilingMiddle.SetActive(value: false);
        }

        public void ShowCeiling()
        {
            ceilingTop.SetActive(value: true);
            ceilingGlass.SetActive(value: false);
            ceilingMiddle.SetActive(value: false);
        }

        public void HideCeiling()
        {
            ceilingTop.SetActive(value: false);
            ceilingGlass.SetActive(value: false);
            ceilingMiddle.SetActive(value: true);
        }

        public bool IsBottomPiece()
        {
            return lowerPiece == null;
        }

        public WaterParkPiece GetBottomPiece()
        {
            if (IsBottomPiece())
            {
                return this;
            }
            return lowerPiece.GetBottomPiece();
        }

        public WaterPark GetWaterParkModule()
        {
            if (IsBottomPiece())
            {
                if (waterParkModule == null)
                {
                    waterParkModule = GetModule();
                }
                return waterParkModule;
            }
            return GetBottomPiece().GetWaterParkModule();
        }

        private WaterPark GetModule()
        {
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null)
            {
                IBaseModule module = componentInParent.GetModule(geometryFace);
                if (module != null)
                {
                    return module as WaterPark;
                }
            }
            return null;
        }

        public bool CanDeconstruct(out string reason)
        {
            reason = null;
            WaterPark waterPark = GetWaterParkModule();
            if (waterPark != null && Player.main.currentWaterPark == waterPark)
            {
                reason = Language.main.Get("PlayerObstacle");
                return false;
            }
            return true;
        }
    }
}
