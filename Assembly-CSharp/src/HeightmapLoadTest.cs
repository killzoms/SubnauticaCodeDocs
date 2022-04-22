using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AssemblyCSharp
{
    public class HeightmapLoadTest : MonoBehaviour
    {
        private static readonly Int2[] batches = new Int2[6]
        {
            new Int2(12, 11),
            new Int2(12, 12),
            new Int2(12, 13),
            new Int2(13, 11),
            new Int2(13, 12),
            new Int2(13, 13)
        };

        private string[] heightmapFiles;

        private byte[] bufferBytes;

        private ushort[] heightLoadBuffer;

        private void LoadFileWithBufferSize(string fileName, int bufferSize)
        {
            using BinaryReader binaryReader = new BinaryReader(FileUtils.ReadFile(fileName));
            binaryReader.ReadInt32();
            binaryReader.ReadInt32();
            int num = 51200;
            bufferBytes = new byte[bufferSize];
            if (heightLoadBuffer == null || heightLoadBuffer.Length != num / 2)
            {
                heightLoadBuffer = new ushort[num / 2];
            }
            int num2 = 0;
            int num3 = 0;
            while ((num3 = binaryReader.Read(bufferBytes, 0, bufferSize)) != 0)
            {
                Buffer.BlockCopy(bufferBytes, 0, heightLoadBuffer, num2, num3);
                num2 += num3;
            }
        }

        private void Start()
        {
            string path = SNUtils.InsideUnmanaged("Build18");
            heightmapFiles = batches.Select((Int2 p) => LargeWorldStreamer.GetBatchHeightmapPath(path, p)).ToArray();
        }

        private void Update()
        {
            ProfilingUtils.BeginSample("BufSize-1024");
            for (int i = 0; i < heightmapFiles.Length; i++)
            {
                LoadFileWithBufferSize(heightmapFiles[i], 1024);
            }
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("BufSize-4096");
            for (int j = 0; j < heightmapFiles.Length; j++)
            {
                LoadFileWithBufferSize(heightmapFiles[j], 4096);
            }
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("BufSize-8192");
            for (int k = 0; k < heightmapFiles.Length; k++)
            {
                LoadFileWithBufferSize(heightmapFiles[k], 8192);
            }
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("BufSize-16384");
            for (int l = 0; l < heightmapFiles.Length; l++)
            {
                LoadFileWithBufferSize(heightmapFiles[l], 16384);
            }
            ProfilingUtils.EndSample();
        }
    }
}
