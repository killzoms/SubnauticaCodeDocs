using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_ResourceTracker : MonoBehaviour
    {
        private class Blip
        {
            public GameObject gameObject;

            public RectTransform rect;

            public Text text;

            public TechType techType;
        }

        [AssertNotNull]
        public GameObject mainCanvas;

        [AssertNotNull]
        public GameObject blip;

        private bool showGUI;

        private readonly List<Blip> blips = new List<Blip>();

        private readonly HashSet<ResourceTracker.ResourceInfo> nodes = new HashSet<ResourceTracker.ResourceInfo>();

        private readonly List<TechType> techTypes = new List<TechType>();

        private readonly List<MapRoomFunctionality> mapRooms = new List<MapRoomFunctionality>();

        private bool showAll;

        private bool gatherNextTick;

        private RectTransform blipRect;

        private RectTransform canvasRect;

        private void Start()
        {
            blipRect = blip.GetComponent<RectTransform>();
            canvasRect = mainCanvas.GetComponent<RectTransform>();
            InvokeRepeating("UpdateVisibility", Random.value, 0.1f);
            DevConsole.RegisterConsoleCommand(this, "showresources");
            InvokeRepeating("GatherNodes", Random.value, 10f);
            ResourceTracker.onResourceRemoved += OnResourceRemoved;
        }

        private void OnDestroy()
        {
            ResourceTracker.onResourceRemoved -= OnResourceRemoved;
        }

        public void OnResourceRemoved(ResourceTracker.ResourceInfo info)
        {
            gatherNextTick = true;
        }

        private void OnConsoleCommand_showresources()
        {
            showAll = !showAll;
            ErrorMessage.AddDebug("showresources = " + showAll);
        }

        private void UpdateVisibility()
        {
            showGUI = showAll || uGUI_CameraDrone.main.GetCamera() != null || (Inventory.main != null && Inventory.main.equipment.GetCount(TechType.MapRoomHUDChip) > 0);
            mainCanvas.SetActive(showGUI);
        }

        private void GatherNodes()
        {
            if (showAll)
            {
                GatherAll();
            }
            else if (showGUI)
            {
                GatherScanned();
            }
        }

        private void GatherAll()
        {
            Camera camera = MainCamera.camera;
            nodes.Clear();
            techTypes.Clear();
            ResourceTracker.GetTechTypesInRange(camera.transform.position, 500f, techTypes);
            for (int i = 0; i < techTypes.Count; i++)
            {
                TechType techType = techTypes[i];
                ResourceTracker.GetNodes(camera.transform.position, 500f, techType, nodes);
            }
        }

        private void GatherScanned()
        {
            Camera camera = MainCamera.camera;
            nodes.Clear();
            mapRooms.Clear();
            MapRoomScreen screen = uGUI_CameraDrone.main.GetScreen();
            if (screen != null)
            {
                mapRooms.Add(screen.mapRoomFunctionality);
            }
            else
            {
                MapRoomFunctionality.GetMapRoomsInRange(camera.transform.position, 500f, mapRooms);
            }
            for (int i = 0; i < mapRooms.Count; i++)
            {
                if (mapRooms[i].GetActiveTechType() != 0)
                {
                    mapRooms[i].GetDiscoveredNodes(nodes);
                }
            }
        }

        private void UpdateBlips()
        {
            Camera camera = MainCamera.camera;
            Vector3 position = camera.transform.position;
            Vector3 forward = camera.transform.forward;
            int num = 0;
            HashSet<ResourceTracker.ResourceInfo>.Enumerator enumerator = nodes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ResourceTracker.ResourceInfo current = enumerator.Current;
                if (Vector3.Dot(current.position - position, forward) > 0f)
                {
                    if (num >= blips.Count)
                    {
                        GameObject gameObject = Object.Instantiate(this.blip, Vector3.zero, Quaternion.identity);
                        RectTransform component = gameObject.GetComponent<RectTransform>();
                        component.SetParent(canvasRect, worldPositionStays: false);
                        component.localScale = blipRect.localScale;
                        Blip blip = new Blip();
                        blip.gameObject = gameObject;
                        blip.rect = component;
                        blip.text = gameObject.GetComponentInChildren<Text>();
                        blip.techType = TechType.None;
                        blips.Add(blip);
                    }
                    Blip blip2 = blips[num];
                    blip2.gameObject.SetActive(value: true);
                    Vector2 vector = camera.WorldToViewportPoint(current.position);
                    blip2.rect.anchorMin = vector;
                    blip2.rect.anchorMax = vector;
                    if (blip2.techType != current.techType)
                    {
                        string text = Language.main.Get(current.techType.AsString());
                        blip2.text.text = text;
                        blip2.techType = current.techType;
                    }
                    num++;
                }
            }
            for (int i = num; i < blips.Count; i++)
            {
                blips[i].gameObject.SetActive(value: false);
            }
        }

        private void LateUpdate()
        {
            UpdateBlips();
            if (gatherNextTick)
            {
                GatherScanned();
                gatherNextTick = false;
            }
        }
    }
}
