using UnityEngine;

namespace AssemblyCSharp
{
    public interface uGUI_IAdjustReceiver
    {
        bool OnAdjust(Vector2 adjustDelta);
    }
}
