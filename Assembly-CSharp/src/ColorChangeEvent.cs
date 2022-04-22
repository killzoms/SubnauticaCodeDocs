using System;
using UnityEngine.Events;

namespace AssemblyCSharp
{
    [Serializable]
    public class ColorChangeEvent : UnityEvent<ColorChangeEventData>
    {
    }
}
