using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MainMenuLoadMenu : MonoBehaviour, uGUI_INavigableIconGrid, uGUI_IButtonReceiver
    {
        public Sprite normalSprite;

        public Sprite selectedSprite;

        public Scrollbar scrollbar;

        public Transform content;

        private GameObject selectedItem;

        public bool isLoading { get; set; }

        public object GetSelectedItem()
        {
            return selectedItem;
        }

        public Graphic GetSelectedIcon()
        {
            return null;
        }

        public void SelectItem(object item)
        {
            DeselectItem();
            selectedItem = item as GameObject;
            selectedItem.transform.GetChild(0).GetComponent<Image>().sprite = selectedSprite;
            mGUI_Change_Legend_On_Select componentInChildren = selectedItem.GetComponentInChildren<mGUI_Change_Legend_On_Select>();
            if ((bool)componentInChildren)
            {
                componentInChildren.SyncLegendBarToGUISelection();
            }
            UIUtils.ScrollToShowItemInCenter(selectedItem.transform);
            Text[] componentsInChildren = selectedItem.GetComponentsInChildren<Text>();
            foreach (Text text in componentsInChildren)
            {
                if (text.gameObject.name != "SaveGameMode")
                {
                    text.color = Color.black;
                }
            }
        }

        public void DeselectItem()
        {
            if (!(selectedItem == null))
            {
                selectedItem.transform.GetChild(0).GetComponent<Image>().sprite = normalSprite;
                Text[] componentsInChildren = selectedItem.GetComponentsInChildren<Text>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].color = Color.white;
                }
                selectedItem = null;
            }
        }

        public bool SelectFirstItem()
        {
            for (int i = 0; i < content.childCount; i++)
            {
                if (content.GetChild(i).gameObject.activeInHierarchy)
                {
                    SelectItem(content.GetChild(i).gameObject);
                    return true;
                }
            }
            return false;
        }

        public bool SelectItemClosestToPosition(Vector3 worldPos)
        {
            return false;
        }

        public bool SelectItemInDirection(int dirX, int dirY)
        {
            if (selectedItem == null)
            {
                return SelectFirstItem();
            }
            if (dirY == 0)
            {
                return false;
            }
            int siblingIndex = selectedItem.transform.GetSiblingIndex();
            int num = ((dirY > 0) ? (siblingIndex + 1) : (siblingIndex - 1));
            int num2 = ((dirY > 0) ? 1 : (-1));
            for (int i = num; i >= 0 && i < content.childCount; i += num2)
            {
                GameObject gameObject = content.GetChild(i).gameObject;
                if (gameObject.activeInHierarchy)
                {
                    SelectItem(gameObject);
                    return true;
                }
            }
            return false;
        }

        public uGUI_INavigableIconGrid GetNavigableGridInDirection(int dirX, int dirY)
        {
            return null;
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            switch (button)
            {
                case GameInput.Button.UISubmit:
                    OnConfirm();
                    return true;
                case GameInput.Button.UIClear:
                    OnClear();
                    return true;
                case GameInput.Button.UICancel:
                    OnBack();
                    return true;
                default:
                    return false;
            }
        }

        public void OnClear()
        {
            MainMenuLoadButton component = selectedItem.GetComponent<MainMenuLoadButton>();
            if (component != null)
            {
                component.RequestDelete();
            }
        }

        public void OnConfirm()
        {
            if (!(selectedItem != null))
            {
                return;
            }
            if (selectedItem.gameObject.name == "NewGame")
            {
                selectedItem.GetComponentInChildren<Button>().onClick.Invoke();
                return;
            }
            MainMenuLoadButton component = selectedItem.GetComponent<MainMenuLoadButton>();
            if (!component.IsEmpty())
            {
                component.Load();
            }
        }

        public void OnBack()
        {
            MainMenuRightSide.main.OpenGroup("Home");
        }
    }
}
