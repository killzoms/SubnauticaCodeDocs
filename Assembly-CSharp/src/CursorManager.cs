using UnityEngine.EventSystems;

namespace AssemblyCSharp
{
    public static class CursorManager
    {
        private static RaycastResult lastRaycastResult;

        public static RaycastResult lastRaycast => lastRaycastResult;

        public static void SetRaycastResult(RaycastResult raycastResult)
        {
            lastRaycastResult = raycastResult;
        }
    }
}
