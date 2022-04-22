using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UWE
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct EditModeScopeTimer : IDisposable
    {
        public EditModeScopeTimer(string label)
        {
            if (!Application.isPlaying)
            {
                ProfilingTimer.Begin(label);
            }
        }

        public void Dispose()
        {
            if (!Application.isPlaying)
            {
                ProfilingTimer.End();
            }
        }
    }
}
