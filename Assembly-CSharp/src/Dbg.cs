using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class Dbg
    {
        private static GUIContent _content = new GUIContent();

        private static GUIStyle _label;

        private static Vector3[] sWorldCornersArray = new Vector3[4];

        public static GUIStyle styleLabel
        {
            get
            {
                if (_label == null)
                {
                    return _label = GUI.skin.label;
                }
                return _label;
            }
        }

        public static string LogHierarchy(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return LogHierarchy();
            }
            return LogHierarchy(gameObject.GetComponent<Transform>());
        }

        public static string LogHierarchy(Transform transform = null)
        {
            if (transform == null)
            {
                return "null";
            }
            Stack<Transform> stack = new Stack<Transform>();
            while (transform != null)
            {
                stack.Push(transform);
                transform = transform.parent;
            }
            StringBuilder stringBuilder = new StringBuilder();
            int i = 0;
            for (int count = stack.Count; i < count; i++)
            {
                transform = stack.Pop();
                stringBuilder.Append(' ', i);
                stringBuilder.AppendLine(transform.name);
            }
            return stringBuilder.ToString();
        }

        public static void DrawText(string s)
        {
            DrawText(10f, 10f, s);
        }

        public static void DrawText(float x, float y, string s)
        {
            _content.text = s;
            Vector2 vector = styleLabel.CalcSize(_content);
            GUI.Label(new Rect(x, y, vector.x, vector.y), s);
        }

        public static void DrawTextCenter(string s)
        {
            _content.text = s;
            Vector2 vector = styleLabel.CalcSize(_content);
            GUI.Label(new Rect(0.5f * ((float)Screen.width - vector.x), 0.5f * ((float)Screen.height - vector.y), vector.x, vector.y), s);
        }

        public static void TraceTarget()
        {
            float maxDist = 100f;
            global::UWE.Utils.TraceForFPSTarget(Player.main.gameObject, maxDist, 0.15f, out var closestObj, out var _);
            DrawText(LogHierarchy(closestObj));
        }

        public static string LogTree(TreeNode tree)
        {
            string text = string.Empty;
            using IEnumerator<TreeNode> enumerator = tree.Traverse();
            while (enumerator.MoveNext())
            {
                TreeNode current = enumerator.Current;
                text += new string('\t', current.depth);
                text += current.id;
                text += "\n";
            }
            return text;
        }

        public static T RandomEnumValue<T>() where T : struct, IConvertible
        {
            int index;
            int length;
            return RandomEnumValue<T>(out index, out length);
        }

        public static T RandomEnumValue<T>(out int index, out int length) where T : struct, IConvertible
        {
            index = -1;
            length = -1;
            Type typeFromHandle = typeof(T);
            if (typeFromHandle.IsEnum)
            {
                Array values = Enum.GetValues(typeFromHandle);
                length = values.Length;
                index = global::UnityEngine.Random.Range(0, length - 1);
                return (T)values.GetValue(index);
            }
            Debug.LogError("T must be an enumerated type");
            return default(T);
        }

        public static int PlainEnumIndex(Enum instance, out int length)
        {
            length = -1;
            Array values = Enum.GetValues(instance.GetType());
            length = values.Length;
            for (int i = 0; i < length; i++)
            {
                if (values.GetValue(i).Equals(instance))
                {
                    return i;
                }
            }
            return -1;
        }

        public static void HighlightRect(RectTransform rectTransform, Color color, float duration)
        {
            if (!(rectTransform == null))
            {
                rectTransform.GetWorldCorners(sWorldCornersArray);
                Debug.DrawLine(sWorldCornersArray[0], sWorldCornersArray[1], color, duration);
                Debug.DrawLine(sWorldCornersArray[1], sWorldCornersArray[2], color, duration);
                Debug.DrawLine(sWorldCornersArray[2], sWorldCornersArray[3], color, duration);
                Debug.DrawLine(sWorldCornersArray[3], sWorldCornersArray[0], color, duration);
            }
        }
    }
}
