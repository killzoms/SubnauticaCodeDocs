using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class LavaLizard : Creature
    {
        public GameObject digInEffect;

        public GameObject digOutEffect;

        [AssertNotNull]
        public LavaDatabase lavaDatabase;

        [AssertNotNull]
        public GameObject model;

        [AssertNotNull]
        public LavaShell lavaShell;

        [AssertNotNull]
        public OnGroundTracker onGroundTracker;

        private Vector3 modelOffset = Vector3.up * -3.1f;

        private Vector3 modelStartPosition;

        public VFXController lavaMoveTrail;

        public FMOD_StudioEventEmitter diveSound;

        private float onGroundFraction;

        private bool wasInLava;

        private bool lavaFXPlaying;

        protected override void InitializeOnce()
        {
            base.InitializeOnce();
            if (Random.value < 0.5f && GetComponent<WaterParkCreature>() == null)
            {
                lavaShell.Activate();
            }
        }

        public override void Start()
        {
            base.Start();
            if (model != null)
            {
                modelStartPosition = model.transform.localPosition;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            InvokeRepeating("UpdateFindLeashPosition", Random.value, 0.5f);
        }

        private bool FindLeashPosition()
        {
            bool result = false;
            Vector3 origin = ((!(Vector3.Distance(base.transform.position, leashPosition) < 10f)) ? (leashPosition + Vector3.up) : (base.transform.position + Random.onUnitSphere + Vector3.up));
            if (global::UWE.Utils.TraceForTerrain(new Ray(origin, Vector3.down), 3000f, out var hitInfo) && lavaDatabase.IsLava(hitInfo.point, hitInfo.normal))
            {
                leashPosition = hitInfo.point;
                result = true;
            }
            return result;
        }

        private void UpdateFindLeashPosition()
        {
            if (FindLeashPosition())
            {
                CancelInvoke("UpdateFindLeashPosition");
            }
        }

        public void Update()
        {
            GetAnimator();
            bool flag = onGroundTracker.onSurface && lavaDatabase.IsLava(onGroundTracker.groundPoint, onGroundTracker.surfaceNormal);
            float num = 0f;
            num = ((!flag) ? (-1f) : 1f);
            onGroundFraction = Mathf.Clamp01(onGroundFraction + Time.deltaTime * num * 0.75f);
            if (onGroundFraction < 1f && onGroundFraction > 0f)
            {
                model.transform.localPosition = modelStartPosition + modelOffset * onGroundFraction;
            }
            if (wasInLava != flag)
            {
                GameObject gameObject = null;
                if (flag)
                {
                    gameObject = digInEffect;
                    Utils.PlayEnvSound(diveSound);
                    GetComponent<Rigidbody>().freezeRotation = true;
                }
                else
                {
                    gameObject = digOutEffect;
                    lavaMoveTrail.Play(1);
                    lavaMoveTrail.Play(2);
                    GetComponent<Rigidbody>().freezeRotation = false;
                    lavaShell.Activate();
                }
                if (gameObject != null)
                {
                    GameObject obj = Object.Instantiate(gameObject);
                    obj.transform.position = onGroundTracker.groundPoint;
                    obj.transform.forward = onGroundTracker.surfaceNormal;
                }
                wasInLava = flag;
            }
            bool flag2 = flag;
            if (flag2 != lavaFXPlaying)
            {
                if (flag2)
                {
                    lavaMoveTrail.Play(0);
                    lavaMoveTrail.StopAndDestroy(1, 6f);
                    lavaMoveTrail.StopAndDestroy(2, 3f);
                }
                else
                {
                    lavaMoveTrail.StopAndDestroy(0, 6f);
                }
                lavaFXPlaying = flag2;
            }
        }
    }
}
