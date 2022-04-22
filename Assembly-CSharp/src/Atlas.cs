using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;

namespace AssemblyCSharp
{
    public class Atlas : ScriptableObject
    {
        [Serializable]
        public class SerialData
        {
            public string name;

            public global::UnityEngine.Sprite sprite;
        }

        public class Sprite
        {
            public Vector2 size;

            public Texture2D texture;

            public float pixelsPerUnit;

            public Vector2[] vertices;

            public Vector2[] uv0;

            public ushort[] triangles;

            public bool slice9Grid;

            public Vector4 padding;

            public Vector4 border;

            public Vector4 inner;

            public Vector4 outer;

            public Sprite(global::UnityEngine.Sprite unitySprite, bool slice9Grid = false)
            {
                size = unitySprite.rect.size;
                texture = unitySprite.texture;
                pixelsPerUnit = unitySprite.pixelsPerUnit;
                border = unitySprite.border;
                if (border.sqrMagnitude > 0f)
                {
                    padding = DataUtility.GetPadding(unitySprite);
                    inner = DataUtility.GetInnerUV(unitySprite);
                    outer = DataUtility.GetOuterUV(unitySprite);
                    vertices = null;
                    uv0 = null;
                    triangles = null;
                }
                else
                {
                    padding = (inner = (outer = Vector4.zero));
                    vertices = unitySprite.vertices;
                    uv0 = unitySprite.uv;
                    triangles = unitySprite.triangles;
                }
                this.slice9Grid = slice9Grid;
            }

            public Sprite(Texture2D texture)
            {
                if (texture == null)
                {
                    texture = Texture2D.whiteTexture;
                }
                size = new Vector2(texture.width, texture.height);
                this.texture = texture;
                pixelsPerUnit = 100f;
                Vector2 vector = new Vector2(0.5f, 0.5f);
                Vector2 vector2 = new Vector2((0f - vector.x) * size.x, (0f - vector.y) * size.y) / pixelsPerUnit;
                Vector2 vector3 = new Vector2((1f - vector.x) * size.x, (1f - vector.y) * size.y) / pixelsPerUnit;
                vertices = new Vector2[4]
                {
                    new Vector2(vector2.x, vector2.y),
                    new Vector2(vector2.x, vector3.y),
                    new Vector2(vector3.x, vector3.y),
                    new Vector2(vector3.x, vector2.y)
                };
                uv0 = new Vector2[4]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f)
                };
                triangles = new ushort[6] { 0, 1, 2, 2, 3, 0 };
                slice9Grid = false;
            }
        }

        public const string sAtlasesFullPath = "Assets/uGUI/Resources/Atlases/";

        public const string sAtlasesPath = "Atlases/";

        private static Dictionary<string, Atlas> nameToAtlas;

        [SerializeField]
        private string atlasName;

        [SerializeField]
        private List<SerialData> serialData;

        private Dictionary<string, Sprite> _nameToSprite;

        private Dictionary<string, Sprite> nameToSprite
        {
            get
            {
                PreWarm(forced: false);
                return _nameToSprite;
            }
        }

        public void PreWarm(bool forced)
        {
            if (!forced && _nameToSprite != null)
            {
                return;
            }
            int num = ((serialData != null) ? serialData.Count : 0);
            _nameToSprite = new Dictionary<string, Sprite>(num, StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < num; i++)
            {
                SerialData obj = serialData[i];
                string text = obj.name;
                global::UnityEngine.Sprite sprite = obj.sprite;
                if (sprite == null)
                {
                    Debug.LogErrorFormat("Sprite with name '{0}' has no UnitySprite", text);
                    continue;
                }
                Sprite value = new Sprite(sprite);
                if (!_nameToSprite.ContainsKey(text))
                {
                    _nameToSprite.Add(text, value);
                }
            }
        }

        public Sprite GetSprite(string name)
        {
            if (nameToSprite.TryGetValue(name, out var value))
            {
                return value;
            }
            return null;
        }

        public void GetNames(ref List<string> names)
        {
            int count = nameToSprite.Count;
            if (names == null)
            {
                names = new List<string>(count);
            }
            else if (names.Capacity < count)
            {
                names.Capacity = count;
            }
            Dictionary<string, Sprite>.Enumerator enumerator = nameToSprite.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, Sprite> current = enumerator.Current;
                names.Add(current.Key);
            }
        }

        public static void InitAtlases()
        {
            if (nameToAtlas == null)
            {
                nameToAtlas = new Dictionary<string, Atlas>();
                Atlas[] array = Resources.LoadAll<Atlas>("Atlases/");
                int i = 0;
                for (int num = array.Length; i < num; i++)
                {
                    Atlas atlas = array[i];
                    nameToAtlas.Add(atlas.atlasName, atlas);
                }
            }
        }

        public static Atlas GetAtlas(string atlasName)
        {
            InitAtlases();
            nameToAtlas.TryGetValue(atlasName, out var value);
            return value;
        }

        public static Sprite GetSprite(string atlasName, string name)
        {
            Atlas atlas = GetAtlas(atlasName);
            if (atlas == null)
            {
                return null;
            }
            return atlas.GetSprite(name);
        }

        public static bool GetNames(string atlasName, ref List<string> names)
        {
            Atlas atlas = GetAtlas(atlasName);
            if (atlas == null)
            {
                return false;
            }
            atlas.GetNames(ref names);
            return true;
        }
    }
}
