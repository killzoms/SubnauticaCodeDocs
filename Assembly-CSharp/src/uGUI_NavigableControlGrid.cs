using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_NavigableControlGrid : MonoBehaviour, uGUI_INavigableIconGrid
    {
        public bool showSelector;

        private Selectable selectedItem;

        private GameObject dummyButton;

        public uGUI_InterGridNavigation interGridNavigation;

        private static List<Selectable> selectableResults = new List<Selectable>();

        private void InitDummyButton()
        {
            if (!dummyButton)
            {
                dummyButton = new GameObject();
                dummyButton.name = "DummyButton";
                dummyButton.AddComponent<Button>().navigation = new Navigation
                {
                    mode = Navigation.Mode.None
                };
            }
        }

        public object GetSelectedItem()
        {
            return selectedItem;
        }

        public Graphic GetSelectedIcon()
        {
            if (showSelector && selectedItem != null)
            {
                if (selectedItem.targetGraphic != null)
                {
                    return selectedItem.targetGraphic;
                }
                return selectedItem.GetComponentInChildren<Graphic>();
            }
            return null;
        }

        public void SelectItem(object item)
        {
            DeselectItem();
            if (item is Selectable)
            {
                selectedItem = item as Selectable;
                selectedItem.Select();
                ScrollToShowSelected();
            }
        }

        public void DeselectItem()
        {
            if (!(selectedItem == null))
            {
                InitDummyButton();
                dummyButton.GetComponent<Button>().Select();
                selectedItem = null;
            }
        }

        public bool SelectFirstItem()
        {
            GetComponentsInChildren(selectableResults);
            for (int i = 0; i < selectableResults.Count; i++)
            {
                Selectable selectable = selectableResults[i];
                if (selectable.navigation.mode != 0)
                {
                    SelectItem(selectable);
                    break;
                }
            }
            selectableResults.Clear();
            return selectedItem != null;
        }

        public bool SelectItemClosestToPosition(Vector3 worldPos)
        {
            float num = float.PositiveInfinity;
            Selectable selectable = null;
            GetComponentsInChildren(selectableResults);
            for (int i = 0; i < selectableResults.Count; i++)
            {
                Selectable selectable2 = selectableResults[i];
                RectTransform component = selectable2.GetComponent<RectTransform>();
                float sqrMagnitude = (component.TransformPoint(component.rect.center) - worldPos).sqrMagnitude;
                if (sqrMagnitude < num)
                {
                    num = sqrMagnitude;
                    selectable = selectable2;
                }
            }
            selectableResults.Clear();
            if (selectable != null)
            {
                SelectItem(selectable);
                return true;
            }
            return false;
        }

        public bool SelectItemInDirection(int dirX, int dirY)
        {
            if (selectedItem == null)
            {
                return SelectFirstItem();
            }
            Selectable selectable = selectedItem;
            do
            {
                switch (dirY)
                {
                    case -1:
                        selectable = selectable.FindSelectableOnUp();
                        break;
                    case 1:
                        selectable = selectable.FindSelectableOnDown();
                        break;
                }
                if (selectable != null)
                {
                    switch (dirX)
                    {
                        case -1:
                            selectable = selectable.FindSelectableOnLeft();
                            break;
                        case 1:
                            selectable = selectable.FindSelectableOnRight();
                            break;
                    }
                }
            }
            while (selectable != null && !selectable.IsActive() && selectable.navigation.mode == Navigation.Mode.Explicit);
            if (selectable != null && selectable != selectedItem && selectable.GetComponentInParent<uGUI_INavigableIconGrid>() == this)
            {
                SelectItem(selectable);
                return true;
            }
            return false;
        }

        public virtual uGUI_INavigableIconGrid GetNavigableGridInDirection(int dirX, int dirY)
        {
            return interGridNavigation.GetNavigableGridInDirection(dirX, dirY);
        }

        private void ScrollToShowSelected()
        {
            if (selectedItem != null)
            {
                UIUtils.ScrollToShowItemInCenter(selectedItem.transform);
            }
        }
    }
}
