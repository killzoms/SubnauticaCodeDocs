using UnityEngine.EventSystems;

namespace AssemblyCSharp
{
    public interface IColorChangeHandler : IEventSystemHandler
    {
        void OnColorChange(ColorChangeEventData eventData);
    }
}
