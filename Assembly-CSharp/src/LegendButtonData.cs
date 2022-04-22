using System;
using UnityEngine;

namespace AssemblyCSharp
{
    [Serializable]
    public struct LegendButtonData
    {
        public int legendButtonIdx;

        public GameInput.Button button;

        [Tooltip("This string will be localized depending on platform language.")]
        [AssertNotNull]
        public string buttonDescription;
    }
}
