using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_Icon : MaskableGraphic
    {
        private static readonly Vector4 sDefaultTangent = new Vector4(1f, 0f, 0f, -1f);

        private static readonly Vector3 sDefaultNormal = new Vector3(0f, 0f, -1f);

        protected static readonly Vector2[] sVertScratch = new Vector2[4];

        protected static readonly Vector2[] sUV0Scratch = new Vector2[4];

        protected Atlas.Sprite _sprite;

        protected bool _fillCenter = true;

        public Atlas.Sprite sprite
        {
            get
            {
                return _sprite;
            }
            set
            {
                if (_sprite != value)
                {
                    _sprite = value;
                    SetAllDirty();
                }
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (_sprite != null)
                {
                    Texture2D texture = _sprite.texture;
                    if (texture != null)
                    {
                        return texture;
                    }
                }
                return Graphic.s_WhiteTexture;
            }
        }

        public float pixelsPerUnit
        {
            get
            {
                float num = ((sprite != null) ? sprite.pixelsPerUnit : 100f);
                float num2 = ((base.canvas != null) ? base.canvas.referencePixelsPerUnit : 100f);
                return num / num2;
            }
        }

        public bool fillCenter
        {
            get
            {
                return _fillCenter;
            }
            set
            {
                if (_fillCenter != value)
                {
                    _fillCenter = value;
                    SetAllDirty();
                }
            }
        }

        protected uGUI_Icon()
        {
            base.useLegacyMeshGeneration = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (sprite == null)
            {
                GenerateQuadMesh(vh);
            }
            else if (sprite.vertices == null)
            {
                GenerateSlice9Mesh(vh, sprite.padding, sprite.border, sprite.outer, sprite.inner);
            }
            else
            {
                GeneratePackedMesh(vh);
            }
        }

        protected void GenerateQuadMesh(VertexHelper vh)
        {
            _ = pixelsPerUnit;
            Rect pixelAdjustedRect = GetPixelAdjustedRect();
            Vector2 vector = new Vector2(pixelAdjustedRect.x, pixelAdjustedRect.y);
            Vector2 vector2 = new Vector2(pixelAdjustedRect.x + pixelAdjustedRect.width, pixelAdjustedRect.y + pixelAdjustedRect.height);
            Vector2 uv0Min = new Vector2(0f, 0f);
            Vector2 uv0Max = new Vector2(1f, 1f);
            Vector2 uv1Min = vector;
            Vector2 uv1Max = vector2;
            vh.Clear();
            AddQuad(vh, vector, vector2, color, uv0Min, uv0Max, uv1Min, uv1Max);
        }

        protected void GeneratePackedMesh(VertexHelper vh)
        {
            Vector2[] vertices = sprite.vertices;
            Vector2[] uv = sprite.uv0;
            ushort[] triangles = sprite.triangles;
            Vector2 size = sprite.size;
            Rect pixelAdjustedRect = GetPixelAdjustedRect();
            vh.Clear();
            Vector2 b = new Vector2(pixelAdjustedRect.width / size.x, pixelAdjustedRect.height / size.y) * sprite.pixelsPerUnit;
            Color32 color = this.color;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 a = vertices[i];
                a = Vector2.Scale(a, b);
                vh.AddVert(new Vector3(a.x, a.y, 0f), color, uv[i], a, sDefaultNormal, sDefaultTangent);
            }
            for (int j = 0; j < triangles.Length; j += 3)
            {
                vh.AddTriangle(triangles[j], triangles[j + 1], triangles[j + 2]);
            }
        }

        protected void GenerateSlice9Mesh(VertexHelper vh, Vector4 padding, Vector4 border, Vector4 outer, Vector4 inner)
        {
            if (!(border.sqrMagnitude > 0f))
            {
                GenerateQuadMesh(vh);
                return;
            }
            float num = pixelsPerUnit;
            Rect pixelAdjustedRect = GetPixelAdjustedRect();
            Vector4 adjustedBorders = GetAdjustedBorders(border / num, pixelAdjustedRect);
            padding /= num;
            sVertScratch[0].x = padding.x;
            sVertScratch[0].y = padding.y;
            sVertScratch[1].x = adjustedBorders.x;
            sVertScratch[1].y = adjustedBorders.y;
            sVertScratch[2].x = pixelAdjustedRect.width - adjustedBorders.z;
            sVertScratch[2].y = pixelAdjustedRect.height - adjustedBorders.w;
            sVertScratch[3].x = pixelAdjustedRect.width - padding.z;
            sVertScratch[3].y = pixelAdjustedRect.height - padding.w;
            for (int i = 0; i < 4; i++)
            {
                sVertScratch[i].x += pixelAdjustedRect.x;
                sVertScratch[i].y += pixelAdjustedRect.y;
            }
            sUV0Scratch[0] = new Vector2(outer.x, outer.y);
            sUV0Scratch[1] = new Vector2(inner.x, inner.y);
            sUV0Scratch[2] = new Vector2(inner.z, inner.w);
            sUV0Scratch[3] = new Vector2(outer.z, outer.w);
            vh.Clear();
            Color32 color = this.color;
            for (int j = 0; j < 3; j++)
            {
                int num2 = j + 1;
                for (int k = 0; k < 3; k++)
                {
                    if (_fillCenter || j != 1 || k != 1)
                    {
                        int num3 = k + 1;
                        Vector2 vector = new Vector2(sVertScratch[j].x, sVertScratch[k].y) * num;
                        Vector2 vector2 = new Vector2(sVertScratch[num2].x, sVertScratch[num3].y) * num;
                        Vector2 uv0Min = new Vector2(sUV0Scratch[j].x, sUV0Scratch[k].y);
                        Vector2 uv0Max = new Vector2(sUV0Scratch[num2].x, sUV0Scratch[num3].y);
                        Vector2 uv1Min = vector;
                        Vector2 uv1Max = vector2;
                        AddQuad(vh, vector, vector2, color, uv0Min, uv0Max, uv1Min, uv1Max);
                    }
                }
            }
        }

        protected static void AddQuad(VertexHelper vh, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uv0Min, Vector2 uv0Max, Vector2 uv1Min, Vector2 uv1Max)
        {
            int currentVertCount = vh.currentVertCount;
            vh.AddVert(new Vector3(posMin.x, posMin.y, 0f), color, new Vector2(uv0Min.x, uv0Min.y), new Vector2(uv1Min.x, uv1Min.y), sDefaultNormal, sDefaultTangent);
            vh.AddVert(new Vector3(posMin.x, posMax.y, 0f), color, new Vector2(uv0Min.x, uv0Max.y), new Vector2(uv1Min.x, uv1Max.y), sDefaultNormal, sDefaultTangent);
            vh.AddVert(new Vector3(posMax.x, posMax.y, 0f), color, new Vector2(uv0Max.x, uv0Max.y), new Vector2(uv1Max.x, uv1Max.y), sDefaultNormal, sDefaultTangent);
            vh.AddVert(new Vector3(posMax.x, posMin.y, 0f), color, new Vector2(uv0Max.x, uv0Min.y), new Vector2(uv1Max.x, uv1Min.y), sDefaultNormal, sDefaultTangent);
            vh.AddTriangle(currentVertCount, currentVertCount + 1, currentVertCount + 2);
            vh.AddTriangle(currentVertCount + 2, currentVertCount + 3, currentVertCount);
        }

        protected Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
        {
            for (int i = 0; i <= 1; i++)
            {
                float num = border[i] + border[i + 2];
                float num2 = rect.size[i];
                if (num2 < num && num != 0f)
                {
                    float num3 = num2 / num;
                    border[i] *= num3;
                    border[i + 2] *= num3;
                }
            }
            return border;
        }

        public override void SetNativeSize()
        {
            if (_sprite != null)
            {
                Vector2 sizeDelta = _sprite.size / pixelsPerUnit;
                base.rectTransform.anchorMax = base.rectTransform.anchorMin;
                base.rectTransform.sizeDelta = sizeDelta;
                SetAllDirty();
            }
        }
    }
}
