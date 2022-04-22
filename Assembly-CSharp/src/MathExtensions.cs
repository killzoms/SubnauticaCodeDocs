using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class MathExtensions
    {
        public const float HALFPI = (float)Math.PI / 2f;

        public const float fixedDeltaTime = 0.02f;

        public const int maxPhysicsIterations = 20;

        private static List<char> _invaidFileChars;

        private static List<char> invalidFileChars
        {
            get
            {
                if (_invaidFileChars == null)
                {
                    _invaidFileChars = new List<char>(Path.GetInvalidFileNameChars());
                    _invaidFileChars.AddRange(Path.GetInvalidPathChars());
                }
                return _invaidFileChars;
            }
        }

        public static Vector3 RotateVectorAroundAxisAngle(Vector3 n, Vector3 v, float a)
        {
            n.Normalize();
            float num = Mathf.Cos(a);
            float num2 = Mathf.Sin(a);
            return v * num + Vector3.Dot(v, n) * n * (1f - num) + Vector3.Cross(n, v) * num2;
        }

        public static float RepeatAngle(float angle)
        {
            return angle - 360f * (1f + Mathf.Floor((angle - 180f) / 360f));
        }

        public static int EncodeIndex(int x, int y)
        {
            return (x + y) * (x + y + 1) / 2 + y;
        }

        public static void Normalize(List<float> values)
        {
            int count = values.Count;
            if (values != null && count != 0)
            {
                float num = 0f;
                for (int i = 0; i < count; i++)
                {
                    float num3 = (values[i] = Mathf.Max(0f, values[i]));
                    num += num3;
                }
                for (int j = 0; j < count; j++)
                {
                    values[j] /= num;
                }
            }
        }

        public static Vector2 FrustumSize(Camera cam, float distance)
        {
            return FrustumSize(cam.fieldOfView, cam.aspect, distance);
        }

        public static Vector2 FrustumSize(float fieldOfView, float aspect, float distance)
        {
            Vector2 result = default(Vector2);
            result.y = distance * Mathf.Tan(fieldOfView * 0.5f * ((float)Math.PI / 180f));
            result.x = result.y * aspect;
            return result;
        }

        public static float SmoothValue(float value, float factor)
        {
            value = Mathf.Clamp01(value);
            factor = Mathf.Clamp01(factor);
            return 1f - value + 2f * Mathf.Sqrt(value) * factor - factor * factor;
        }

        public static float EaseInSine(float value)
        {
            return Mathf.Sin((value - 1f) * (float)Math.PI * 0.5f) + 1f;
        }

        public static float EaseOutSine(float value)
        {
            return Mathf.Sin(value * (float)Math.PI * 0.5f);
        }

        public static float EvaluateLine(float x1, float y1, float x2, float y2, float x)
        {
            float num = (y2 - y1) / (x2 - x1);
            float num2 = y1 - num * x1;
            return num * x + num2;
        }

        public static void Oscillation(float reduction, float frequency, float seed, float t, out float o, out float o1)
        {
            seed = Mathf.Clamp01(seed);
            float num = 0.5f * Mathf.Pow(reduction, 0f - t);
            float num2 = Mathf.Sin((t * frequency + seed) * 2f * (float)Math.PI);
            o = num * (1f + num2);
            o1 = num * (1f - num2);
        }

        public static void Spring(ref float velocity, ref float current, float target, float coef, float dT, float velocityDamp, float velocityMax = -1f)
        {
            float num = target - current;
            float num2 = coef * num;
            velocity = (velocity + num2 * dT) * velocityDamp;
            if (velocityMax > 0f)
            {
                float num3 = ((velocity >= 0f) ? 1f : (-1f));
                if (velocity * num3 > velocityMax)
                {
                    velocity = velocityMax * num3;
                }
            }
            current += velocity * dT;
        }

        public static void UniqueRandomNumbersInRange(int min, int max, int count, ref List<int> numbers)
        {
            if (count <= 0)
            {
                return;
            }
            if (numbers.Capacity < count)
            {
                numbers.Capacity = count;
            }
            if (max - min < count)
            {
                Debug.LogError("MathExtensions.UniqueRandomNumbersInRange : Specified range is below required numbers count! Make sure that (max - min) >= count");
                return;
            }
            int num = 0;
            while (num < count)
            {
                int item = global::UnityEngine.Random.Range(min, max);
                if (!numbers.Contains(item))
                {
                    numbers.Add(item);
                    num++;
                }
            }
        }

        public static string Color2Hex(Color c, float alpha = 1f)
        {
            int num = Mathf.RoundToInt(255f * c.r);
            int num2 = Mathf.RoundToInt(255f * c.g);
            int num3 = Mathf.RoundToInt(255f * c.b);
            int num4 = Mathf.RoundToInt(255f * c.a * alpha);
            return $"{num:X2}{num2:X2}{num3:X2}{num4:X2}";
        }

        public static Texture2D ScaleTexture(Texture2D src, int width, int height, bool mipmap, bool linear = false)
        {
            Texture2D texture2D = new Texture2D(width, height, src.format, mipmap, linear);
            texture2D.name = "MathExtensions.ScaleTexture";
            texture2D.wrapMode = TextureWrapMode.Clamp;
            Color[] array = new Color[width * height];
            float num = 1f / (float)width;
            float num2 = 1f / (float)height;
            float num3 = 0f;
            float num4 = 0f;
            num3 = 0.5f;
            num4 = 0.5f;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = src.GetPixelBilinear(num * ((float)(i % width) + num3), num2 * ((float)(i / width) + num4));
            }
            texture2D.SetPixels(array);
            texture2D.Apply();
            return texture2D;
        }

        public static Texture2D ScaleTexture(Texture2D src, int width, bool mipmap, bool linear = false)
        {
            float num = (float)src.height / (float)src.width;
            return ScaleTexture(src, width, Mathf.FloorToInt(num * (float)width), mipmap, linear);
        }

        public static Texture2D LoadTexture(byte[] bytes)
        {
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false);
            texture2D.name = "MathExtensions.LoadTexture";
            if (!texture2D.LoadImage(bytes))
            {
                return null;
            }
            texture2D.wrapMode = TextureWrapMode.Clamp;
            return texture2D;
        }

        public static string FilterNonFileChars(string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in input)
            {
                if (!invalidFileChars.Contains(c))
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString();
        }

        public static string GetUniqueFileName(DirectoryInfo dirInfo, string prefix, string extension, int numberOfDigits, bool startFromOne, bool dense)
        {
            if (dirInfo == null || !dirInfo.Exists)
            {
                return null;
            }
            string searchPattern = prefix + "*." + extension;
            string text = "D" + numberOfDigits;
            numberOfDigits = Mathf.Clamp(numberOfDigits, 1, 20);
            prefix = FilterNonFileChars(prefix);
            extension = FilterNonFileChars(extension);
            int num = (startFromOne ? 1 : 0);
            int num2 = -1;
            FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
            List<int> list = new List<int>();
            int length = prefix.Length;
            int i = 0;
            for (int num3 = files.Length; i < num3; i++)
            {
                string name = files[i].Name;
                string text2 = name.Substring(length, name.LastIndexOf('.') - length);
                int result = -1;
                if (text2.Length >= numberOfDigits && int.TryParse(text2, out result) && result >= 0)
                {
                    list.Add(result);
                    if (result > num2)
                    {
                        num2 = result;
                    }
                }
            }
            if (dense)
            {
                int count = list.Count;
                if (count > 1)
                {
                    list.Sort();
                    int num4 = num;
                    for (int j = 0; j < count; j++)
                    {
                        int num5 = list[j];
                        int num6 = num5 - num4;
                        if (num6 < 0)
                        {
                            num = num4;
                            break;
                        }
                        if (num6 > 1)
                        {
                            num = num4 + 1;
                            break;
                        }
                        if (num6 == 1)
                        {
                            if (j == count - 1)
                            {
                                num = num5 + 1;
                            }
                            else
                            {
                                num4 = num5;
                            }
                        }
                    }
                }
                else if (count == 1 && num == list[0])
                {
                    num++;
                }
            }
            else
            {
                num = Mathf.Max(num, num2 + 1);
            }
            return prefix + num.ToString(text) + "." + extension;
        }

        public static void RectFit(float width, float height, float parentWidth, float parentHeight, RectScaleMode mode, out Vector2 scale, out Vector2 offset)
        {
            RectFit(width, height, parentWidth / parentHeight, mode, out scale, out offset);
        }

        public static void RectFit(float width, float height, float parentAspect, RectScaleMode mode, out Vector2 scale, out Vector2 offset)
        {
            float num = width / height;
            switch (mode)
            {
                case RectScaleMode.Fit:
                    if (num > parentAspect)
                    {
                        scale = new Vector2(1f, num / parentAspect);
                    }
                    else
                    {
                        scale = new Vector2(parentAspect / num, 1f);
                    }
                    break;
                case RectScaleMode.Envelope:
                    if (num > parentAspect)
                    {
                        scale = new Vector2(parentAspect / num, 1f);
                    }
                    else
                    {
                        scale = new Vector2(1f, num / parentAspect);
                    }
                    break;
                default:
                    scale = new Vector2(1f, 1f);
                    break;
            }
            offset = new Vector2((1f - scale.x) * 0.5f, (1f - scale.y) * 0.5f);
        }
    }
}
