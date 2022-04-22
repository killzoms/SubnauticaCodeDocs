using System;
using UnityEngine;
using UnityEngine.Events;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Collider))]
    public class OnTouch : MonoBehaviour
    {
        [Serializable]
        public class OnTouchEvent : UnityEvent<Collider>
        {
        }

        public string tagFilter;

        public OnTouchEvent onTouch;

        private void OnTriggerEnter(Collider collider)
        {
            if ((string.IsNullOrEmpty(tagFilter) || collider.gameObject.CompareTag(tagFilter)) && (!collider.isTrigger || collider.gameObject.layer == LayerMask.NameToLayer("Useable")))
            {
                onTouch.Invoke(collider);
            }
        }
    }
}
