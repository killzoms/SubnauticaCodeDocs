using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class AcidicBrineDamageTrigger : MonoBehaviour, IManagedUpdateBehaviour, IManagedBehaviour
    {
        private List<GameObject> targets = new List<GameObject>();

        private int currentIndex;

        [AssertNotNull]
        public BoxCollider box;

        public int managedUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "AcidicBrineDamageTrigger";
        }

        private void OnDisable()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        private void OnDestroy()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        private bool IsValidTarget(LiveMixin liveMixin)
        {
            bool flag = liveMixin != null && liveMixin.IsAlive() && !DamageSystem.IsAcidImmune(liveMixin.gameObject);
            if (flag && liveMixin.gameObject.GetComponent<Player>() != null && Player.main.currentSub != null)
            {
                flag = false;
            }
            return flag;
        }

        private bool Contains(Vector3 point)
        {
            Vector3 vector = box.transform.InverseTransformPoint(point);
            Vector3 v = box.center - vector;
            Vector3 vector2 = box.size * 0.5f + Vector3.one;
            return v.InBox(-vector2, vector2);
        }

        private void RemoveTarget(GameObject target)
        {
            if (targets.Contains(target))
            {
                AcidicBrineDamage component = target.GetComponent<AcidicBrineDamage>();
                if (component != null)
                {
                    component.Decrement();
                }
                targets.Remove(target);
                RequestUpdateIfNecessary();
            }
        }

        private void AddTarget(GameObject target)
        {
            if (!targets.Contains(target))
            {
                AcidicBrineDamage acidicBrineDamage = target.GetComponent<AcidicBrineDamage>();
                if (acidicBrineDamage == null)
                {
                    acidicBrineDamage = target.AddComponent<AcidicBrineDamage>();
                }
                acidicBrineDamage.Increment();
                targets.Add(target);
                RequestUpdateIfNecessary();
            }
        }

        private LiveMixin GetLiveMixin(GameObject go)
        {
            GameObject gameObject = global::UWE.Utils.GetEntityRoot(go);
            if (gameObject == null)
            {
                gameObject = go;
            }
            if (go.GetComponentInChildren<IgnoreTrigger>() != null)
            {
                return null;
            }
            if ((bool)gameObject.GetComponent<SubRoot>())
            {
                return null;
            }
            return gameObject.GetComponent<LiveMixin>();
        }

        private void OnTriggerEnter(Collider other)
        {
            LiveMixin liveMixin = GetLiveMixin(other.gameObject);
            if (IsValidTarget(liveMixin))
            {
                AddTarget(liveMixin.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            LiveMixin liveMixin = GetLiveMixin(other.gameObject);
            if ((bool)liveMixin)
            {
                RemoveTarget(liveMixin.gameObject);
            }
        }

        public void ManagedUpdate()
        {
            if (targets.Count <= 0)
            {
                return;
            }
            if (currentIndex >= targets.Count)
            {
                currentIndex = 0;
            }
            if (targets[currentIndex] == null)
            {
                targets.RemoveAt(currentIndex);
                return;
            }
            Vector3 position = targets[currentIndex].transform.position;
            if (!Contains(position))
            {
                RemoveTarget(targets[currentIndex]);
            }
            else
            {
                currentIndex++;
            }
        }

        private void RequestUpdateIfNecessary()
        {
            if (targets.Count != 0)
            {
                BehaviourUpdateUtils.Register(this);
            }
            else
            {
                BehaviourUpdateUtils.Deregister(this);
            }
        }
    }
}
