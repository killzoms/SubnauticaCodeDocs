using UnityEngine.EventSystems;

namespace AssemblyCSharp
{
    public interface IPointerHoverHandler : IEventSystemHandler
    {
        void OnPointerHover(PointerEventData eventData);
    }
}
