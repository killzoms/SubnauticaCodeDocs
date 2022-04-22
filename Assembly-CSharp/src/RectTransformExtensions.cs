using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public static class RectTransformExtensions
    {
        private static List<Canvas> sCanvases = new List<Canvas>();

        private static Vector3[] sFourCornersArray = new Vector3[4];

        private static Vector2[] sFourPointsArray = new Vector2[4];

        public static Canvas GetCanvas(GameObject go)
        {
            Canvas result = null;
            sCanvases.Clear();
            go.GetComponentsInParent(includeInactive: false, sCanvases);
            if (sCanvases.Count > 0)
            {
                for (int i = 0; i < sCanvases.Count; i++)
                {
                    if (sCanvases[i].isActiveAndEnabled)
                    {
                        result = sCanvases[i];
                        break;
                    }
                }
            }
            sCanvases.Clear();
            return result;
        }

        public static void ExpandRect(RectTransform rt)
        {
            rt.sizeDelta = Vector2.zero;
            rt.localPosition = Vector3.zero;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        public static void CenterRect(RectTransform rt, Vector2 anchor)
        {
            anchor.x = Mathf.Clamp01(anchor.x);
            anchor.y = Mathf.Clamp01(anchor.y);
            rt.sizeDelta = Vector2.zero;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            Rect rect = rt.parent.GetComponent<RectTransform>().rect;
            rt.anchoredPosition = new Vector2(rect.width * (0.5f - anchor.x), rect.height * (0.5f - anchor.y));
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        public static void SetParams(RectTransform rt, Vector2 anchor, Vector2 pivot, Transform parent = null)
        {
            rt.pivot = pivot;
            if (parent != null)
            {
                rt.SetParent(parent, worldPositionStays: false);
            }
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.localRotation = Quaternion.identity;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
        }

        public static void SetSize(RectTransform rt, float width, float height)
        {
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public static void Fit(RectTransform rt, float desiredWidth, float desiredHeight, float defaultWidth, float defaultHeight, bool keepAspect)
        {
            Vector2 vector = Fit(desiredWidth, desiredHeight, defaultWidth, defaultHeight, keepAspect);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vector.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vector.y);
        }

        public static Vector2 Fit(float desiredWidth, float desiredHeight, float defaultWidth, float defaultHeight, bool keepAspect)
        {
            float num = defaultWidth / defaultHeight;
            bool num2 = desiredWidth < 0f;
            bool flag = desiredHeight < 0f;
            Vector2 result = default(Vector2);
            if (num2)
            {
                if (flag)
                {
                    result.x = defaultWidth;
                    result.y = defaultHeight;
                }
                else
                {
                    result.x = num * desiredHeight;
                    result.y = desiredHeight;
                }
            }
            else if (flag)
            {
                result.x = desiredWidth;
                result.y = desiredWidth / num;
            }
            else if (keepAspect)
            {
                float num3 = Mathf.Min(desiredWidth / defaultWidth, desiredHeight / defaultHeight);
                result.x = defaultWidth * num3;
                result.y = defaultHeight * num3;
            }
            else
            {
                result.x = desiredWidth;
                result.y = desiredHeight;
            }
            return result;
        }

        public static void SetSize(RectTransform rt, float maxWidth, float maxHeight, float width, float height)
        {
            float num = Mathf.Min(maxWidth / width, maxHeight / height);
            SetSize(rt, width * num, height * num);
        }

        public static bool GetRect(RectTransform src, Transform dst, out Rect rect)
        {
            if (src != null && dst != null)
            {
                Rect rect2 = src.rect;
                Vector3 position = new Vector3(rect2.x, rect2.y, 0f);
                Vector3 position2 = new Vector3(rect2.xMax, rect2.yMax, 0f);
                position = src.TransformPoint(position);
                position2 = src.TransformPoint(position2);
                position = dst.InverseTransformPoint(position);
                position2 = dst.InverseTransformPoint(position2);
                if (position.x > position2.x)
                {
                    global::UWE.Utils.Swap(ref position.x, ref position2.x);
                }
                if (position.y > position2.y)
                {
                    global::UWE.Utils.Swap(ref position.y, ref position2.y);
                }
                rect = new Rect(position.x, position.y, position2.x - position.x, position2.y - position.y);
                return true;
            }
            rect = default(Rect);
            return false;
        }

        public static bool GetCanvasRect(Graphic graphic, out Rect rect)
        {
            if (graphic != null)
            {
                Canvas canvas = graphic.canvas;
                if (canvas != null)
                {
                    return GetRect(graphic.rectTransform, canvas.transform, out rect);
                }
            }
            rect = default(Rect);
            return false;
        }

        public static Vector2 GetPointOnRectEdge(Rect rect, Vector2 dir, float normalizedDistance = 0.95f)
        {
            normalizedDistance = Mathf.Clamp01(normalizedDistance);
            if (dir != Vector2.zero)
            {
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            }
            dir = rect.center + Vector2.Scale(rect.size, dir * 0.5f * normalizedDistance);
            return dir;
        }

        public static Vector2 GetPointOnRectEdge(RectTransform rt, Vector2 dir, float normalizedDistance = 0.95f)
        {
            if (rt == null)
            {
                return Vector2.zero;
            }
            return GetPointOnRectEdge(rt.rect, dir, normalizedDistance);
        }

        public static bool IsAncestorOf(this Transform child, Transform ancestor)
        {
            if (ancestor == null)
            {
                return false;
            }
            Transform transform = child;
            while (transform != null)
            {
                if (ancestor == transform)
                {
                    return true;
                }
                transform = transform.parent;
            }
            return false;
        }

        public static Vector2 GetAnchorPoint(UIAnchor anchor)
        {
            return anchor switch
            {
                UIAnchor.LowerLeft => new Vector2(0f, 0f), 
                UIAnchor.LowerCenter => new Vector2(0.5f, 0f), 
                UIAnchor.LowerRight => new Vector2(1f, 0f), 
                UIAnchor.MiddleLeft => new Vector2(0f, 0.5f), 
                UIAnchor.MiddleCenter => new Vector2(0.5f, 0.5f), 
                UIAnchor.MiddleRight => new Vector2(1f, 0.5f), 
                UIAnchor.UpperLeft => new Vector2(0f, 1f), 
                UIAnchor.UpperCenter => new Vector2(0.5f, 1f), 
                UIAnchor.UpperRight => new Vector2(1f, 1f), 
                _ => Vector2.zero, 
            };
        }

        public static Vector2 ClosestPoint(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 vector = b - a;
            Vector2 lhs = p - a;
            float sqrMagnitude = vector.sqrMagnitude;
            if (sqrMagnitude == 0f)
            {
                return a;
            }
            float num = Mathf.Clamp01(Vector2.Dot(lhs, vector) / sqrMagnitude);
            return a + num * vector;
        }

        public static Vector3 ClosestPoint(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 vector = b - a;
            Vector3 lhs = p - a;
            float sqrMagnitude = vector.sqrMagnitude;
            if (sqrMagnitude == 0f)
            {
                return a;
            }
            float num = Mathf.Clamp01(Vector3.Dot(lhs, vector) / sqrMagnitude);
            return a + num * vector;
        }

        public static Vector3 ClosestPoint(this RectTransform rt, Vector3 worldPos)
        {
            rt.GetWorldCorners(sFourCornersArray);
            Vector3 result = Vector3.zero;
            float num = float.PositiveInfinity;
            for (int i = 0; i < 4; i++)
            {
                int num2 = i;
                int num3 = i + 1;
                if (num3 == 4)
                {
                    num3 = 0;
                }
                Vector3 vector = ClosestPoint(sFourCornersArray[num2], sFourCornersArray[num3], worldPos);
                float sqrMagnitude = (worldPos - vector).sqrMagnitude;
                if (sqrMagnitude < num)
                {
                    num = sqrMagnitude;
                    result = vector;
                }
            }
            return result;
        }

        public static Vector2 ClosestPoint(this Rect rect, Vector2 position)
        {
            float xMin = rect.xMin;
            float yMin = rect.yMin;
            float xMax = rect.xMax;
            float yMax = rect.yMax;
            sFourPointsArray[0].x = xMin;
            sFourPointsArray[0].y = yMin;
            sFourPointsArray[1].x = xMin;
            sFourPointsArray[1].y = yMax;
            sFourPointsArray[2].x = xMax;
            sFourPointsArray[2].y = yMax;
            sFourPointsArray[3].x = xMax;
            sFourPointsArray[3].y = yMin;
            Vector2 result = Vector2.zero;
            float num = float.PositiveInfinity;
            for (int i = 0; i < 4; i++)
            {
                int num2 = i;
                int num3 = i + 1;
                if (num3 == 4)
                {
                    num3 = 0;
                }
                Vector2 vector = ClosestPoint(sFourPointsArray[num2], sFourPointsArray[num3], position);
                float sqrMagnitude = (position - vector).sqrMagnitude;
                if (sqrMagnitude < num)
                {
                    num = sqrMagnitude;
                    result = vector;
                }
            }
            return result;
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawDebugRect(RectTransform rt, Vector2 min, Vector2 max, Color color, bool diagonal = false)
        {
            Vector3 vector = rt.TransformPoint(new Vector3(min.x, min.y, 0f));
            Vector3 vector2 = rt.TransformPoint(new Vector3(min.x, max.y, 0f));
            Vector3 vector3 = rt.TransformPoint(new Vector3(max.x, max.y, 0f));
            Vector3 vector4 = rt.TransformPoint(new Vector3(max.x, min.y, 0f));
            global::UnityEngine.Debug.DrawLine(vector, vector2, color);
            global::UnityEngine.Debug.DrawLine(vector2, vector3, color);
            global::UnityEngine.Debug.DrawLine(vector3, vector4, color);
            global::UnityEngine.Debug.DrawLine(vector4, vector, color);
            if (diagonal)
            {
                global::UnityEngine.Debug.DrawLine(vector, vector3, color);
            }
        }
    }
}
