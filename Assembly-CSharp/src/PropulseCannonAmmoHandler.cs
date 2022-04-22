using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PropulseCannonAmmoHandler : MonoBehaviour, IProtoEventListener
    {
        public GameObject fxTrailPrefab;

        private GameObject fxTrailInstance;

        private const float maxTime = 3f;

        private const int currentVersion = 1;

        private DealDamageOnImpact damageOnImpact;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public bool addedDamageOnImpact;

        [NonSerialized]
        [ProtoMember(3)]
        public bool behaviorWasEnabled;

        [NonSerialized]
        [ProtoMember(4)]
        public bool wasShot;

        [NonSerialized]
        [ProtoMember(5)]
        public float timeShot = -1f;

        [NonSerialized]
        [ProtoMember(6)]
        public bool locomotionWasEnabled;

        [NonSerialized]
        [ProtoMember(7)]
        public Vector3 velocity;

        private CollisionDetectionMode collisionDetectionMode;

        private List<Collider> disabledColliders = new List<Collider>();

        private PropulsionCannon cannon;

        private bool selfDestruct;

        private float shotDetectionModeDelayTime = 7f;

        private void Start()
        {
            Rigidbody component = GetComponent<Rigidbody>();
            if (!base.gameObject.GetComponent<SetRigidBodyModeAfterDelay>())
            {
                collisionDetectionMode = component.collisionDetectionMode;
            }
            component.collisionDetectionMode = CollisionDetectionMode.Continuous;
            LargeWorldEntity component2 = GetComponent<LargeWorldEntity>();
            if (component2 != null)
            {
                base.transform.parent = null;
                LargeWorldStreamer.main.cellManager.UnregisterEntity(component2);
            }
            if (Application.isEditor)
            {
                base.gameObject.AddComponent<DebugDrawAABB>();
            }
        }

        public void ResetHandler(bool disableColliders = false, bool deserializing = false)
        {
            if (!deserializing)
            {
                CleanUpHandler();
            }
            Rigidbody component = GetComponent<Rigidbody>();
            if (component.useGravity)
            {
                Debug.LogWarningFormat(this, "Propulsion Cannon ammo '{0}' is using gravity. Disabling now.", base.gameObject);
                component.useGravity = false;
            }
            if (GetComponent<WorldForces>() == null)
            {
                Debug.LogWarningFormat(this, "Propulsion Cannon ammo '{0}' is missing WorldForces component. Adding one now.", base.gameObject);
                base.gameObject.AddComponent<WorldForces>().useRigidbody = component;
            }
            IPropulsionCannonAmmo[] components = base.gameObject.GetComponents<IPropulsionCannonAmmo>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].OnGrab();
            }
            Living component2 = GetComponent<Living>();
            if (component2 != null && (!deserializing || !(component2 is Creature)))
            {
                behaviorWasEnabled = component2.enabled;
                component2.enabled = false;
            }
            Locomotion component3 = GetComponent<Locomotion>();
            if (component3 != null)
            {
                locomotionWasEnabled = component3.enabled;
                component3.enabled = false;
            }
            if (!disableColliders)
            {
                return;
            }
            Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
            for (int j = 0; j < componentsInChildren.Length; j++)
            {
                if (componentsInChildren[j].enabled)
                {
                    componentsInChildren[j].enabled = false;
                    disabledColliders.Add(componentsInChildren[j]);
                }
            }
        }

        public void OnShot(bool deserializing = false)
        {
            for (int i = 0; i < disabledColliders.Count; i++)
            {
                disabledColliders[i].enabled = true;
            }
            disabledColliders.Clear();
            damageOnImpact = GetComponent<DealDamageOnImpact>();
            if (damageOnImpact == null)
            {
                damageOnImpact = base.gameObject.AddComponent<DealDamageOnImpact>();
                addedDamageOnImpact = true;
            }
            IPropulsionCannonAmmo[] components = base.gameObject.GetComponents<IPropulsionCannonAmmo>();
            for (int j = 0; j < components.Length; j++)
            {
                components[j].OnShoot();
            }
            if (fxTrailPrefab != null)
            {
                fxTrailInstance = Utils.SpawnPrefabAt(fxTrailPrefab, null, base.transform.position);
                if (fxTrailInstance != null)
                {
                    fxTrailInstance.SetActive(value: true);
                    ParticleSystem component = fxTrailInstance.GetComponent<ParticleSystem>();
                    if (component != null)
                    {
                        component.Play();
                    }
                }
            }
            if (!deserializing)
            {
                wasShot = true;
                timeShot = DayNightCycle.main.timePassedAsFloat;
                LargeWorldEntity component2 = GetComponent<LargeWorldEntity>();
                if (component2 != null)
                {
                    LargeWorldStreamer.main.cellManager.RegisterEntity(component2);
                }
            }
        }

        private void TriggerAmmoEndEvent()
        {
            IPropulsionCannonAmmo[] components = base.gameObject.GetComponents<IPropulsionCannonAmmo>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].OnRelease();
            }
        }

        private void TriggerAmmoImpactEvent()
        {
            IPropulsionCannonAmmo[] components = base.gameObject.GetComponents<IPropulsionCannonAmmo>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].OnImpact();
            }
        }

        private void CleanUpHandler()
        {
            for (int i = 0; i < disabledColliders.Count; i++)
            {
                disabledColliders[i].enabled = true;
            }
            if (addedDamageOnImpact && damageOnImpact != null)
            {
                global::UnityEngine.Object.Destroy(damageOnImpact);
            }
            if (behaviorWasEnabled)
            {
                Living component = GetComponent<Living>();
                if (component != null)
                {
                    component.enabled = true;
                }
            }
            if (locomotionWasEnabled)
            {
                Locomotion component2 = GetComponent<Locomotion>();
                if (component2 != null)
                {
                    component2.enabled = true;
                }
            }
            damageOnImpact = null;
            addedDamageOnImpact = false;
            timeShot = -1f;
            behaviorWasEnabled = false;
            wasShot = false;
            locomotionWasEnabled = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (wasShot)
            {
                TriggerAmmoImpactEvent();
                selfDestruct = true;
            }
        }

        private void OnExamine()
        {
            UndoChanges();
            global::UnityEngine.Object.Destroy(this);
        }

        private void Update()
        {
            if (wasShot && fxTrailInstance != null)
            {
                fxTrailInstance.transform.position = base.transform.position;
            }
            if (selfDestruct || (wasShot && (bool)DayNightCycle.main && (double)(timeShot + 3f) <= DayNightCycle.main.timePassed))
            {
                UndoChanges();
                global::UnityEngine.Object.Destroy(this);
            }
        }

        public void SetCannon(PropulsionCannon setCannon)
        {
            cannon = setCannon;
        }

        public void UndoChanges()
        {
            if (wasShot)
            {
                SetRigidBodyModeAfterDelay component = base.gameObject.GetComponent<SetRigidBodyModeAfterDelay>();
                if ((bool)component)
                {
                    component.TriggerStart(shotDetectionModeDelayTime, collisionDetectionMode);
                }
                else
                {
                    base.gameObject.AddComponent<SetRigidBodyModeAfterDelay>().TriggerStart(shotDetectionModeDelayTime, collisionDetectionMode);
                }
            }
            else
            {
                GetComponent<Rigidbody>().collisionDetectionMode = collisionDetectionMode;
            }
            TriggerAmmoEndEvent();
            CleanUpHandler();
            if (cannon != null)
            {
                cannon.OnAmmoHandlerDestroyed(base.gameObject);
            }
            cannon = null;
            Creature component2 = GetComponent<Creature>();
            if (component2 != null)
            {
                component2.leashPosition = base.transform.position;
            }
            LargeWorldEntity component3 = GetComponent<LargeWorldEntity>();
            if (component3 != null)
            {
                Pickupable component4 = GetComponent<Pickupable>();
                if (component4 == null || !component4.attached)
                {
                    LargeWorldStreamer.main.cellManager.RegisterEntity(component3);
                }
            }
            if (Application.isEditor)
            {
                DebugDrawAABB component5 = GetComponent<DebugDrawAABB>();
                if (component5 != null)
                {
                    global::UnityEngine.Object.Destroy(component5);
                }
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            Rigidbody component = GetComponent<Rigidbody>();
            if ((bool)component)
            {
                velocity = component.velocity;
            }
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            Rigidbody component = GetComponent<Rigidbody>();
            if ((bool)component)
            {
                component.isKinematic = false;
                component.useGravity = false;
                component.velocity = velocity;
            }
            if (wasShot)
            {
                ResetHandler(disableColliders: false, deserializing: true);
                OnShot(deserializing: true);
            }
            else
            {
                selfDestruct = true;
            }
        }

        private void OnKill()
        {
            behaviorWasEnabled = false;
            locomotionWasEnabled = false;
        }

        private void OnDisable()
        {
            if (cannon != null)
            {
                cannon.OnAmmoHandlerDestroyed(base.gameObject);
            }
        }
    }
}
