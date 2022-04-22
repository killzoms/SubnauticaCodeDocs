using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UWE;

namespace AssemblyCSharp
{
    public class MainMenuLoadButton : MonoBehaviour
    {
        private enum target
        {
            left,
            right,
            centre
        }

        public string saveGame;

        public int changeSet;

        public GameMode gameMode;

        public GameObject delete;

        public GameObject load;

        public GameObject deleteButton;

        public Selectable cancelDeleteButton;

        public GameObject upgradeWarning;

        private CanvasGroup loadCg;

        private CanvasGroup deleteCg;

        public GameObject contentArea;

        [AssertNotNull]
        public Text[] labelsForColorSwap;

        public float animTime = 0.25f;

        public float alphaPower = 1.5f;

        public float posPower = 2f;

        public float slotAnimTime = 0.3f;

        public float slotPosPower = 1.5f;

        public float shiftDistanace = 25f;

        public float slotShiftDistance = 80f;

        private bool hasShifted;

        private Vector3 centrePos;

        private void Start()
        {
            loadCg = load.GetComponent<CanvasGroup>();
            deleteCg = delete.GetComponent<CanvasGroup>();
            contentArea = base.transform.parent.gameObject;
            Vector3 localPosition = base.gameObject.transform.localPosition;
            Vector3 localPosition2 = new Vector3(localPosition.x, localPosition.y, 0f);
            base.gameObject.transform.localPosition = localPosition2;
        }

        public bool NeedsUpgrade()
        {
            return BatchUpgrade.NeedsUpgrade(changeSet);
        }

        public bool IsEmpty()
        {
            return SaveLoadManager.main.GetGameInfo(saveGame) == null;
        }

        public void Load()
        {
            if (!IsEmpty())
            {
                CoroutineHost.StartCoroutine(uGUI_MainMenu.main.LoadGameAsync(saveGame, changeSet, gameMode));
            }
        }

        public void RequestDelete()
        {
            uGUI_MainMenu.main.OnRightSideOpened(deleteCg.gameObject);
            uGUI_LegendBar.ClearButtons();
            uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
            uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("ItelSelectorSelect"));
            StartCoroutine(ShiftAlpha(loadCg, 0f, animTime, alphaPower, toActive: false));
            StartCoroutine(ShiftAlpha(deleteCg, 1f, animTime, alphaPower, toActive: true, cancelDeleteButton));
            StartCoroutine(ShiftPos(loadCg, target.left, target.centre, animTime, posPower));
            StartCoroutine(ShiftPos(deleteCg, target.centre, target.right, animTime, posPower));
        }

        public void CancelDelete()
        {
            MainMenuRightSide.main.OpenGroup("SavedGames");
            StartCoroutine(ShiftAlpha(loadCg, 1f, animTime, alphaPower, toActive: true));
            StartCoroutine(ShiftAlpha(deleteCg, 0f, animTime, alphaPower, toActive: false));
            StartCoroutine(ShiftPos(loadCg, target.centre, target.left, animTime, posPower));
            StartCoroutine(ShiftPos(deleteCg, target.right, target.centre, animTime, posPower));
        }

        public void Delete()
        {
            MainMenuRightSide.main.OpenGroup("SavedGames");
            StartCoroutine(ShiftPos(deleteCg, target.left, target.centre, animTime, posPower));
            StartCoroutine(ShiftAlpha(deleteCg, 0f, animTime, alphaPower, toActive: false));
            StartCoroutine(FreeSlot(contentArea, slotAnimTime, slotPosPower));
            Debug.Log("Save / Load: User requested deletion of save instance with path " + saveGame);
            CoroutineHost.StartCoroutine(SaveLoadManager.main.ClearSlotAsync(saveGame));
        }

        private IEnumerator ShiftAlpha(CanvasGroup cg, float targetAlpha, float animTime, float power, bool toActive, Selectable buttonToSelect = null)
        {
            float start = Time.time;
            while (Time.time - start < animTime)
            {
                float f = Mathf.Clamp01((Time.time - start) / animTime);
                cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Mathf.Pow(f, power));
                yield return null;
            }
            cg.alpha = targetAlpha;
            if (toActive)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            if (GameInput.IsPrimaryDeviceGamepad() && buttonToSelect != null)
            {
                GamepadInputModule.current.SelectItem(buttonToSelect);
            }
        }

        private IEnumerator ShiftPos(CanvasGroup cg, target target, target origin, float animTime, float power)
        {
            Vector3 targetPos = new Vector3(0f, 0f, 0f);
            Vector3 localPosition = new Vector3(0f, 0f, 0f);
            if (!hasShifted)
            {
                centrePos = cg.transform.localPosition;
                hasShifted = true;
            }
            switch (target)
            {
                case target.left:
                    targetPos = centrePos + new Vector3(0f - shiftDistanace, 0f, 0f);
                    break;
                case target.right:
                    targetPos = centrePos + new Vector3(shiftDistanace, 0f, 0f);
                    break;
                case target.centre:
                    targetPos = centrePos;
                    break;
            }
            switch (origin)
            {
                case target.left:
                    localPosition = centrePos + new Vector3(0f - shiftDistanace, 0f, 0f);
                    break;
                case target.right:
                    localPosition = centrePos + new Vector3(shiftDistanace, 0f, 0f);
                    break;
                case target.centre:
                    localPosition = centrePos;
                    break;
            }
            cg.transform.localPosition = localPosition;
            float start = Time.time;
            while (Time.time - start < animTime)
            {
                float f = Mathf.Clamp01((Time.time - start) / animTime);
                cg.transform.localPosition = Vector3.Lerp(cg.transform.localPosition, targetPos, Mathf.Pow(f, power));
                yield return null;
            }
            cg.transform.localPosition = targetPos;
        }

        private IEnumerator FreeSlot(GameObject ca, float animTime, float power)
        {
            int count = ca.transform.childCount;
            int index = base.gameObject.transform.GetSiblingIndex();
            if (index + 1 == count)
            {
                yield return new WaitForSeconds(animTime);
                Object.Destroy(base.gameObject);
            }
            GridLayoutGroup glg = ca.GetComponent<GridLayoutGroup>();
            ScrollRect sr = ca.transform.parent.GetComponentInParent<ScrollRect>();
            sr.enabled = false;
            glg.enabled = false;
            foreach (Transform item in ca.transform)
            {
                int siblingIndex = item.GetSiblingIndex();
                if (siblingIndex > index)
                {
                    RectTransform component = item.GetComponent<RectTransform>();
                    StartCoroutine(Bump(component, animTime, power));
                    if (siblingIndex + 1 == count)
                    {
                        yield return new WaitForSeconds(animTime);
                    }
                    else
                    {
                        yield return new WaitForSeconds(animTime / 4f);
                    }
                }
            }
            glg.enabled = true;
            sr.enabled = true;
            Object.Destroy(base.gameObject);
        }

        private IEnumerator Bump(RectTransform rt, float animTime, float power)
        {
            float start = Time.time;
            Vector3 targetPos = rt.transform.localPosition + new Vector3(0f, slotShiftDistance, 0f);
            while (Time.time - start < animTime)
            {
                float f = Mathf.Clamp01((Time.time - start) / animTime);
                rt.transform.localPosition = Vector3.Lerp(rt.transform.localPosition, targetPos, Mathf.Pow(f, power));
                yield return null;
            }
            rt.transform.localPosition = targetPos;
        }

        public void onCursorEnter()
        {
            Text[] array = labelsForColorSwap;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].color = Color.black;
            }
        }

        public void onCursorLeave()
        {
            Text[] array = labelsForColorSwap;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].color = Color.white;
            }
        }
    }
}
