using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_MapRoomResourceNode : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
    {
        [HideInInspector]
        public uGUI_MapRoomScanner mainUI;

        [HideInInspector]
        public int index;

        public Text text;

        public GameObject hover;

        public GameObject background;

        public uGUI_Icon icon;

        [HideInInspector]
        public FMODAsset hoverSound;

        private void Start()
        {
            hover.SetActive(value: false);
            background.SetActive(value: true);
        }

        public void SetTechType(TechType techType)
        {
            text.text = Language.main.Get(techType.AsString());
            Atlas.Sprite withNoDefault = SpriteManager.GetWithNoDefault(techType);
            if (withNoDefault != null)
            {
                icon.sprite = withNoDefault;
                icon.enabled = true;
            }
            else
            {
                icon.enabled = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hover.SetActive(value: true);
            background.SetActive(value: false);
            Utils.PlayFMODAsset(hoverSound, base.transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hover.SetActive(value: false);
            background.SetActive(value: true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            mainUI.OnStartScan(index);
            hover.SetActive(value: false);
            background.SetActive(value: true);
        }
    }
}
