using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Gendarme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class uGUI_Tooltip : MonoBehaviour, ICanvasElement
    {
        private static readonly Vector2 margin = new Vector2(10f, 20f);

        private const float paddingX = 20f;

        private const float paddingY = 20f;

        private const float iconWidth = 110f;

        private const int iconsInRow = 3;

        private const float gap = 0.9f;

        private const float maxWidth = 469f;

        private const int poolChunkSize = 3;

        private const ManagedUpdate.Queue updateQueue = ManagedUpdate.Queue.LateUpdateAfterInput;

        private static uGUI_Tooltip main;

        private static string tooltipText;

        private static List<TooltipIcon> tooltipIcons = new List<TooltipIcon>();

        private static bool visible = true;

        public float scaleFactor = 0.001080918f;

        [AssertNotNull]
        public uGUI_TooltipIcon prefabIconEntry;

        [AssertNotNull]
        public RectTransform rectTransform;

        [AssertNotNull]
        public CanvasGroup canvasGroup;

        [AssertNotNull]
        public Image background;

        [AssertNotNull]
        public Text text;

        [AssertNotNull]
        public FlexibleGridLayout iconCanvas;

        private RectTransform backgroundRect;

        private RectTransform textRect;

        private Matrix4x4 worldToLocalMatrix;

        private Matrix4x4 localToWorldMatrix;

        private Vector3 position;

        private Quaternion rotation;

        private Vector3 scale;

        private Rect rect;

        private Vector3 aimingPosition;

        private Vector3 aimingForward;

        private int layer;

        private int cachedTooltipIconsHash = -1;

        private List<uGUI_TooltipIcon> icons = new List<uGUI_TooltipIcon>();

        private void Awake()
        {
            if (main != null)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            main = this;
            backgroundRect = background.rectTransform;
            textRect = text.rectTransform;
            layer = LayerID.UI;
            canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            ManagedUpdate.Subscribe(ManagedUpdate.Queue.LateUpdateAfterInput, OnUpdate);
        }

        private void OnDisable()
        {
            ManagedUpdate.Unsubscribe(ManagedUpdate.Queue.LateUpdateAfterInput, OnUpdate);
        }

        private void OnUpdate()
        {
            if (string.IsNullOrEmpty(tooltipText) || !ExtractParams())
            {
                Clear();
            }
            if (visible)
            {
                canvasGroup.alpha = 1f;
                base.gameObject.layer = layer;
                bool flag = false;
                if (text.text != tooltipText)
                {
                    text.text = tooltipText;
                    flag = true;
                }
                int num = 5351;
                for (int i = 0; i < tooltipIcons.Count; i++)
                {
                    num = 31 * num + tooltipIcons[i].GetHashCode();
                }
                if (num != cachedTooltipIconsHash)
                {
                    cachedTooltipIconsHash = num;
                    flag = true;
                }
                if (flag)
                {
                    int count = tooltipIcons.Count;
                    EnsureIconsCount(count);
                    for (int j = 0; j < icons.Count; j++)
                    {
                        uGUI_TooltipIcon uGUI_TooltipIcon2 = icons[j];
                        if (j < count)
                        {
                            TooltipIcon tooltipIcon = tooltipIcons[j];
                            uGUI_TooltipIcon2.gameObject.SetActive(value: true);
                            uGUI_TooltipIcon2.SetIcon(tooltipIcon.sprite);
                            uGUI_TooltipIcon2.SetText(tooltipIcon.text);
                        }
                        else
                        {
                            uGUI_TooltipIcon2.gameObject.SetActive(value: false);
                        }
                    }
                    CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
                }
                else
                {
                    UpdatePosition();
                }
            }
            else
            {
                canvasGroup.alpha = 0f;
            }
        }

        private bool ExtractParams()
        {
            RaycastResult lastRaycast = CursorManager.lastRaycast;
            GameObject gameObject = lastRaycast.gameObject;
            if (gameObject == null)
            {
                return false;
            }
            Graphic component = gameObject.GetComponent<Graphic>();
            if (component == null)
            {
                return false;
            }
            Canvas canvas = component.canvas;
            if (canvas == null)
            {
                return false;
            }
            RectTransform component2 = canvas.GetComponent<RectTransform>();
            worldToLocalMatrix = component2.worldToLocalMatrix;
            localToWorldMatrix = component2.localToWorldMatrix;
            position = lastRaycast.worldPosition;
            rotation = component2.rotation;
            scale = component2.lossyScale;
            rect = component2.rect;
            uGUI_GraphicRaycaster uGUI_GraphicRaycaster2 = lastRaycast.module as uGUI_GraphicRaycaster;
            Transform transform = ((uGUI_GraphicRaycaster2 != null) ? uGUI_GraphicRaycaster2.eventCamera : MainCamera.camera).transform;
            aimingPosition = transform.position;
            aimingForward = transform.forward;
            layer = canvas.gameObject.layer;
            return true;
        }

        private void UpdatePosition()
        {
            float num = Vector3.Dot(aimingForward, position - aimingPosition) * scaleFactor;
            rectTransform.localScale = new Vector3(num, num, num);
            rectTransform.rotation = rotation;
            Vector2 b = new Vector2(num / scale.x, num / scale.y);
            Vector2 vector = Vector2.Scale(backgroundRect.rect.size, b);
            Vector2 vector2 = Vector2.Scale(margin, b);
            Vector3 vector3 = worldToLocalMatrix.MultiplyPoint3x4(position);
            Vector3 point = new Vector3(vector3.x + vector2.x, vector3.y - vector2.y, vector3.z);
            Vector2 vector4 = rect.position;
            Vector2 vector5 = rect.position + rect.size;
            bool num2 = point.x + vector.x > vector5.x;
            bool flag = point.y - vector.y < vector4.y;
            if (num2)
            {
                point.x = vector3.x - vector2.x - vector.x;
            }
            if (flag)
            {
                point.y = vector3.y + vector2.y + vector.y;
            }
            rectTransform.position = localToWorldMatrix.MultiplyPoint3x4(point);
        }

        private void EnsureIconsCount(int count)
        {
            if (icons.Count < count)
            {
                int num = Math.Max(3, count - icons.Count);
                for (int i = 0; i < num; i++)
                {
                    uGUI_TooltipIcon component = global::UnityEngine.Object.Instantiate(prefabIconEntry.gameObject).GetComponent<uGUI_TooltipIcon>();
                    component.rectTransform.SetParent(iconCanvas.transform, worldPositionStays: false);
                    VerticalLayoutGroup layoutGroup = component.layoutGroup;
                    int num2 = layoutGroup.padding.left + layoutGroup.padding.right;
                    float num3 = 110f - (float)num2;
                    component.SetSize(num3, num3);
                    component.gameObject.SetActive(value: false);
                    icons.Add(component);
                }
            }
        }

        public static void Set(ITooltip tooltip)
        {
            if (tooltip != null)
            {
                tooltipIcons.Clear();
                tooltip.GetTooltip(out tooltipText, tooltipIcons);
                visible = true;
            }
        }

        public static void Clear()
        {
            visible = false;
        }

        [SuppressMessage("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Layout)
            {
                RectTransform rectTransform = iconCanvas.rectTransform;
                float pixelsPerUnit = text.pixelsPerUnit;
                for (int i = 0; i < icons.Count; i++)
                {
                    icons[i].layoutGroup.CalculateLayoutInputHorizontal();
                }
                TextGenerationSettings generationSettings = text.GetGenerationSettings(new Vector2(0f, 0f));
                float a = text.cachedTextGeneratorForLayout.GetPreferredWidth(tooltipText, generationSettings) / pixelsPerUnit;
                iconCanvas.CalculateLayoutInputHorizontal();
                float preferredWidth = iconCanvas.preferredWidth;
                float a2 = Mathf.Max(a, preferredWidth);
                a2 = Mathf.Min(a2, 429f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(preferredWidth, a2));
                textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a2);
                iconCanvas.SetLayoutHorizontal();
                for (int j = 0; j < icons.Count; j++)
                {
                    icons[j].layoutGroup.SetLayoutHorizontal();
                }
                for (int k = 0; k < icons.Count; k++)
                {
                    icons[k].layoutGroup.CalculateLayoutInputVertical();
                }
                TextGenerationSettings generationSettings2 = text.GetGenerationSettings(new Vector2(a2, 0f));
                float preferredHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight(tooltipText, generationSettings2);
                preferredHeight /= pixelsPerUnit;
                iconCanvas.CalculateLayoutInputVertical();
                float num = preferredHeight + iconCanvas.preferredHeight;
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconCanvas.preferredHeight);
                textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
                iconCanvas.SetLayoutVertical();
                for (int l = 0; l < icons.Count; l++)
                {
                    icons[l].layoutGroup.SetLayoutVertical();
                }
                this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a2 + 40f);
                this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num + 40f);
                textRect.anchoredPosition = new Vector2(20f, -20f);
                rectTransform.anchoredPosition = new Vector2(20f + 0.5f * (a2 - rectTransform.rect.width), -20f - preferredHeight);
            }
        }

        public void LayoutComplete()
        {
            UpdatePosition();
        }

        public void GraphicUpdateComplete()
        {
        }

        public bool IsDestroyed()
        {
            return this == null;
        }

        [SpecialName]
        Transform ICanvasElement.transform => transform;
    }
}
