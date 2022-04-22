using UnityEngine;

namespace AssemblyCSharp
{
    public class WaterParkItem : MonoBehaviour
    {
        public Pickupable pickupable;

        public InfectedMixin infectedMixin;

        protected WaterPark currentWaterPark;

        protected virtual void OnAddToWP()
        {
            Rigidbody component = base.gameObject.GetComponent<Rigidbody>();
            if (component != null)
            {
                component.isKinematic = false;
            }
            base.gameObject.SendMessage("OnAddToWaterPark", this, SendMessageOptions.DontRequireReceiver);
        }

        protected virtual void OnRemoveFromWP()
        {
            Object.Destroy(this);
        }

        public virtual void ValidatePosition()
        {
            if (!(currentWaterPark == null))
            {
                Vector3 localPoint = base.transform.localPosition;
                currentWaterPark.EnsureLocalPointIsInside(ref localPoint);
                base.transform.localPosition = localPoint;
            }
        }

        public void SetWaterPark(WaterPark waterPark)
        {
            if (!(waterPark == currentWaterPark))
            {
                WaterPark waterPark2 = currentWaterPark;
                currentWaterPark = waterPark;
                if (waterPark2 != null)
                {
                    waterPark2.RemoveItem(this);
                }
                if (currentWaterPark != null)
                {
                    currentWaterPark.AddItem(this);
                }
                bool num = waterPark2 != null;
                bool flag = currentWaterPark != null;
                if (num && !flag)
                {
                    OnRemoveFromWP();
                }
                if (!num && flag)
                {
                    OnAddToWP();
                }
                if (flag)
                {
                    ValidatePosition();
                }
            }
        }

        public WaterPark GetWaterPark()
        {
            return currentWaterPark;
        }

        public virtual int GetSize()
        {
            CreatureEgg component = GetComponent<CreatureEgg>();
            if (component != null)
            {
                return component.GetCreatureSize();
            }
            return 0;
        }

        public bool IsInsideWaterPark()
        {
            return currentWaterPark != null;
        }

        private void OnDestroy()
        {
            if (currentWaterPark != null)
            {
                currentWaterPark.RemoveItem(this);
            }
        }
    }
}
