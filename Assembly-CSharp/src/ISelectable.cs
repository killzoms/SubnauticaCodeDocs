using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public interface ISelectable
    {
        bool IsValid();

        RectTransform GetRect();

        [SuppressMessage("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        bool OnButtonDown(GameInput.Button button);
    }
}
