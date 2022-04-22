using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_MapRoomScanner : MonoBehaviour
    {
        public uGUI_MapRoomResourceNode[] resourceList;

        public GameObject resourceListRoot;

        public GameObject scanningRoot;

        public Text activeTechTypeLabel;

        public MapRoomFunctionality mapRoom;

        public FMODAsset startScanningSound;

        public FMODAsset cancelScanningSound;

        public FMODAsset hoverSound;

        public Text scanningText;

        public uGUI_GraphicRaycaster raycaster;

        public uGUI_Icon scanningIcon;

        public Text navText;

        public GameObject nextPageButton;

        public GameObject prevPageButton;

        public FMODAsset pageChangeSound;

        private TechType lastActiveTechType;

        private readonly HashSet<TechType> availableTechTypes = new HashSet<TechType>();

        private readonly List<TechType> sortedTechTypes = new List<TechType>();

        private int currentPage;

        private readonly Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        private readonly Color defaultColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        private readonly Color hoverColor = Color.white;

        private int numPages
        {
            get
            {
                float num = sortedTechTypes.Count;
                float num2 = resourceList.Length;
                return Mathf.CeilToInt(num / num2);
            }
        }

        public void Start()
        {
            UpdateGUIState();
            UpdateAvailableTechTypes();
            for (int i = 0; i < resourceList.Length; i++)
            {
                resourceList[i].index = i;
                resourceList[i].mainUI = this;
                resourceList[i].hoverSound = hoverSound;
            }
            ResourceTracker.onResourceDiscovered += OnResourceDiscovered;
            scanningText.text = Language.main.Get("MapRoomScanningText");
        }

        public void HoverNextPage(bool enter)
        {
            if (currentPage < numPages - 1)
            {
                nextPageButton.GetComponent<Image>().color = (enter ? hoverColor : defaultColor);
            }
        }

        public void NextPage()
        {
            if (currentPage < numPages - 1)
            {
                currentPage++;
                RebuildResourceList();
                Utils.PlayFMODAsset(pageChangeSound);
            }
        }

        public void HoverPrevPage(bool enter)
        {
            if (currentPage > 0)
            {
                prevPageButton.GetComponent<Image>().color = (enter ? hoverColor : defaultColor);
            }
        }

        public void PreviousPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                RebuildResourceList();
                Utils.PlayFMODAsset(pageChangeSound);
            }
        }

        private void OnTriggerEnter(Collider c)
        {
            if ((bool)c.GetComponent<Player>())
            {
                raycaster.enabled = true;
            }
        }

        private void OnTriggerExit(Collider c)
        {
            if ((bool)c.GetComponent<Player>())
            {
                raycaster.enabled = false;
            }
        }

        public void OnResourceDiscovered(ResourceTracker.ResourceInfo info)
        {
            if (!availableTechTypes.Contains(info.techType))
            {
                Vector3 vector = mapRoom.transform.position - info.position;
                float scanRange = mapRoom.GetScanRange();
                float num = scanRange * scanRange;
                if (vector.sqrMagnitude <= num)
                {
                    availableTechTypes.Add(info.techType);
                    RebuildResourceList();
                }
            }
        }

        public void OnCancelScan()
        {
            mapRoom.StartScanning(TechType.None);
            UpdateGUIState();
        }

        public void OnStartScan(int index)
        {
            index = Mathf.Clamp(index + currentPage * resourceList.Length, 0, sortedTechTypes.Count - 1);
            TechType newTypeToScan = sortedTechTypes[index];
            mapRoom.StartScanning(newTypeToScan);
            UpdateGUIState();
        }

        private void UpdateAvailableTechTypes()
        {
            availableTechTypes.Clear();
            ResourceTracker.GetTechTypesInRange(mapRoom.transform.position, mapRoom.GetScanRange(), availableTechTypes);
            RebuildResourceList();
        }

        private void RebuildResourceList()
        {
            sortedTechTypes.Clear();
            sortedTechTypes.AddRange(availableTechTypes);
            sortedTechTypes.Sort(CompareByName);
            int num = currentPage * resourceList.Length;
            int num2 = 0;
            int num3 = Mathf.Min(sortedTechTypes.Count, num + resourceList.Length);
            for (int i = num; i < num3; i++)
            {
                TechType techType = sortedTechTypes[i];
                uGUI_MapRoomResourceNode obj = resourceList[num2];
                obj.gameObject.SetActive(value: true);
                obj.SetTechType(techType);
                num2++;
            }
            for (int j = num2; j < resourceList.Length; j++)
            {
                resourceList[j].gameObject.SetActive(value: false);
            }
            navText.text = Language.main.GetFormat("MapRoomPagesFormat", currentPage + 1, numPages);
            prevPageButton.GetComponent<Image>().color = ((currentPage != 0) ? defaultColor : disabledColor);
            nextPageButton.GetComponent<Image>().color = ((currentPage + 1 < numPages) ? defaultColor : disabledColor);
        }

        private static int CompareByName(TechType a, TechType b)
        {
            Language main = Language.main;
            string strA = main.Get(a.AsString());
            string strB = main.Get(b.AsString());
            return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateGUIState()
        {
            TechType activeTechType = mapRoom.GetActiveTechType();
            bool num = lastActiveTechType != activeTechType;
            resourceListRoot.SetActive(activeTechType == TechType.None);
            scanningRoot.SetActive(activeTechType != TechType.None);
            if (num)
            {
                if (activeTechType != 0)
                {
                    Atlas.Sprite withNoDefault = SpriteManager.GetWithNoDefault(activeTechType);
                    if (withNoDefault != null)
                    {
                        scanningIcon.sprite = withNoDefault;
                        scanningIcon.enabled = true;
                    }
                    else
                    {
                        scanningIcon.enabled = false;
                    }
                    StartAnimation();
                    activeTechTypeLabel.text = Language.main.Get(activeTechType.AsString());
                    Utils.PlayFMODAsset(startScanningSound, base.transform);
                }
                else
                {
                    Utils.PlayFMODAsset(cancelScanningSound, base.transform);
                }
            }
            lastActiveTechType = activeTechType;
        }

        private void StartAnimation()
        {
        }

        private void OnDestroy()
        {
            ResourceTracker.onResourceDiscovered -= OnResourceDiscovered;
        }
    }
}
