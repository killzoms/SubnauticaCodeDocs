using ProtoBuf;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class MapRoomScreen : HandTarget, IHandTarget
    {
        [AssertNotNull]
        public string hoverText = "ControllCamera";

        [AssertNotNull]
        public Text cameraText;

        [AssertNotNull]
        public MapRoomFunctionality mapRoomFunctionality;

        private int currentIndex;

        private MapRoomCamera currentCamera;

        public const float maxCameraDistance = 500f;

        private void Start()
        {
            cameraText.text = Language.main.GetFormat("MapRoomCameraInfoScreen", MapRoomCamera.lastCameraNum);
            MapRoomCamera.onMapRoomCameraChanged += OnMapRoomCameraChanged;
        }

        private void OnDestroy()
        {
            MapRoomCamera.onMapRoomCameraChanged -= OnMapRoomCameraChanged;
        }

        public void OnMapRoomCameraChanged()
        {
            cameraText.text = Language.main.GetFormat("MapRoomCameraInfoScreen", MapRoomCamera.lastCameraNum);
        }

        public MapRoomCamera GetCurrentCamera()
        {
            return currentCamera;
        }

        private int NormalizeIndex(int index)
        {
            if (MapRoomCamera.cameras.Count != 0)
            {
                if (index < 0)
                {
                    index += MapRoomCamera.cameras.Count;
                }
                else if (index >= MapRoomCamera.cameras.Count)
                {
                    index %= MapRoomCamera.cameras.Count;
                }
            }
            return index;
        }

        public MapRoomCamera FindCamera(int direction = 1)
        {
            direction = (int)Mathf.Sign(direction);
            for (int i = 0; i < MapRoomCamera.cameras.Count; i++)
            {
                int index = NormalizeIndex(currentIndex + i * direction);
                if (MapRoomCamera.cameras[index].CanBeControlled(this))
                {
                    currentIndex = index;
                    return MapRoomCamera.cameras[index];
                }
            }
            return null;
        }

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText(hoverText);
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
        }

        public void OnHandClick(GUIHand guiHand)
        {
            currentIndex = NormalizeIndex(currentIndex);
            MapRoomCamera mapRoomCamera = FindCamera();
            if ((bool)mapRoomCamera)
            {
                mapRoomCamera.ControlCamera(Player.main, this);
                currentCamera = mapRoomCamera;
            }
        }

        public void OnCameraFree(MapRoomCamera camera)
        {
            if (camera == currentCamera)
            {
                currentCamera = null;
            }
        }

        public void CycleCamera(int direction = 1)
        {
            currentIndex += direction;
            currentIndex = NormalizeIndex(currentIndex);
            MapRoomCamera mapRoomCamera = FindCamera(direction);
            if (mapRoomCamera != null && mapRoomCamera != currentCamera)
            {
                currentCamera.FreeCamera(resetPlayerPosition: false);
                mapRoomCamera.ControlCamera(Player.main, this);
                currentCamera = mapRoomCamera;
            }
        }
    }
}
