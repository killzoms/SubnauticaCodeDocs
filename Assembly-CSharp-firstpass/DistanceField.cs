using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gendarme;
using UnityEngine;

public struct DistanceField
{
    private sealed class SharedDistanceField
    {
        public int referenceCount;

        public DistanceField distanceField;
    }

    public const float valueScale = 5f;

    private const int fileVersion = 1;

    private static readonly Dictionary<string, SharedDistanceField> sharedDistanceFields = new Dictionary<string, SharedDistanceField>();

    public Texture3D texture;

    public Vector3 min;

    public Vector3 max;

    public static IEnumerator LoadSharedAsync(string fileName, IOut<DistanceField> result)
    {
        SharedDistanceField value = null;
        if (sharedDistanceFields.TryGetValue(fileName, out value))
        {
            value.referenceCount++;
            result.Set(value.distanceField);
            yield break;
        }
        ResourceRequest request = Resources.LoadAsync(fileName);
        yield return request;
        TextAsset textAsset = request.asset as TextAsset;
        if (textAsset == null)
        {
            Debug.LogErrorFormat("Couldn't load '{0}'", fileName);
            result.Set(default(DistanceField));
            yield break;
        }
        if (sharedDistanceFields.TryGetValue(fileName, out value))
        {
            value.referenceCount++;
        }
        else
        {
            value = new SharedDistanceField();
            value.referenceCount = 1;
            value.distanceField.texture = LoadTexture(textAsset, out var vector, out var vector2);
            value.distanceField.min = vector;
            value.distanceField.max = vector2;
            sharedDistanceFields.Add(fileName, value);
        }
        result.Set(value.distanceField);
        Resources.UnloadAsset(textAsset);
    }

    public static void UnloadShared(string fileName)
    {
        SharedDistanceField value = null;
        if (sharedDistanceFields.TryGetValue(fileName, out value))
        {
            value.referenceCount--;
            if (value.referenceCount == 0)
            {
                Object.Destroy(value.distanceField.texture);
                sharedDistanceFields.Remove(fileName);
            }
        }
        else
        {
            Debug.LogError("Unloading a shared distance field that wasn't loaded");
        }
    }

    public static Texture3D LoadTexture(string fileName, out Vector3 min, out Vector3 max)
    {
        return LoadTexture(Resources.Load(fileName) as TextAsset, out min, out max);
    }

    [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
    public static Texture3D LoadTexture(TextAsset asset, out Vector3 min, out Vector3 max)
    {
        float[,,] array = Load(asset, out min, out max);
        if (array == null)
        {
            return null;
        }
        int length = array.GetLength(0);
        int length2 = array.GetLength(1);
        int length3 = array.GetLength(2);
        Texture3D texture3D = new Texture3D(length, length2, length3, TextureFormat.Alpha8, mipChain: false);
        texture3D.filterMode = FilterMode.Bilinear;
        texture3D.wrapMode = TextureWrapMode.Clamp;
        Color32[] array2 = new Color32[length * length2 * length3];
        for (int i = 0; i < length3; i++)
        {
            for (int j = 0; j < length2; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    int num = k + (j + i * length2) * length;
                    float num2 = array[k, j, i] / 5f;
                    array2[num].a = (byte)Mathf.Clamp(num2 * 128f + 128f, 0f, 255f);
                }
            }
        }
        texture3D.SetPixels32(array2);
        texture3D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return texture3D;
    }

    public static float[,,] Load(TextAsset asset, out Vector3 min, out Vector3 max)
    {
        using BinaryReader binaryReader = new BinaryReader(new MemoryStream(asset.bytes));
        int num = binaryReader.ReadInt32();
        if (num != 1)
        {
            Debug.LogErrorFormat("Distance field '{0}' was in the wrong format (was {1}, expected {2})", asset.name, num, 1);
            min = Vector3.zero;
            max = Vector3.zero;
            return null;
        }
        int num2 = binaryReader.ReadInt32();
        int num3 = binaryReader.ReadInt32();
        int num4 = binaryReader.ReadInt32();
        min.x = binaryReader.ReadSingle();
        min.y = binaryReader.ReadSingle();
        min.z = binaryReader.ReadSingle();
        max.x = binaryReader.ReadSingle();
        max.y = binaryReader.ReadSingle();
        max.z = binaryReader.ReadSingle();
        float[,,] array = new float[num2, num3, num4];
        for (int i = 0; i < num4; i++)
        {
            for (int j = 0; j < num3; j++)
            {
                for (int k = 0; k < num2; k++)
                {
                    array[k, j, i] = binaryReader.ReadSingle();
                }
            }
        }
        return array;
    }

    public static void Save(string fileName, float[,,] distanceField, Vector3 min, Vector3 max)
    {
        using BinaryWriter binaryWriter = new BinaryWriter(FileUtils.CreateFile(fileName));
        int length = distanceField.GetLength(0);
        int length2 = distanceField.GetLength(1);
        int length3 = distanceField.GetLength(2);
        binaryWriter.Write(1);
        binaryWriter.Write(length);
        binaryWriter.Write(length2);
        binaryWriter.Write(length3);
        binaryWriter.Write(min.x);
        binaryWriter.Write(min.y);
        binaryWriter.Write(min.z);
        binaryWriter.Write(max.x);
        binaryWriter.Write(max.y);
        binaryWriter.Write(max.z);
        for (int i = 0; i < length3; i++)
        {
            for (int j = 0; j < length2; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    binaryWriter.Write(distanceField[k, j, i]);
                }
            }
        }
        binaryWriter.Close();
    }
}
