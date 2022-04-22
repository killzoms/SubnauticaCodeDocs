using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class BreakableResource : HandTarget, IPropulsionCannonAmmo, IHandTarget
    {
        [Serializable]
        public class RandomPrefab
        {
            public GameObject prefab;

            public float chance;
        }

        [AssertNotNull(AssertNotNullAttribute.Options.AllowEmptyCollection)]
        public List<RandomPrefab> prefabList;

        public GameObject defaultPrefab;

        public float verticalSpawnOffset = 0.2f;

        public int numChances = 1;

        public int hitsToBreak;

        [AssertNotNull]
        public FMODAsset hitSound;

        [AssertNotNull]
        public FMODAsset breakSound;

        public GameObject hitFX;

        public GameObject breakFX;

        [AssertNotNull]
        public string breakText = "BreakRock";

        public string customGoalText = "";

        public override void Awake()
        {
            base.Awake();
        }

        void IPropulsionCannonAmmo.OnGrab()
        {
        }

        void IPropulsionCannonAmmo.OnShoot()
        {
        }

        void IPropulsionCannonAmmo.OnImpact()
        {
            BreakIntoResources();
        }

        void IPropulsionCannonAmmo.OnRelease()
        {
        }

        bool IPropulsionCannonAmmo.GetAllowedToGrab()
        {
            return true;
        }

        bool IPropulsionCannonAmmo.GetAllowedToShoot()
        {
            return true;
        }

        private void BashHit()
        {
            BreakIntoResources();
        }

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText(breakText);
            HandReticle.main.SetIcon(HandReticle.IconType.Hand);
        }

        public void OnHandClick(GUIHand hand)
        {
            if (Utils.GetLocalPlayerComp().GetInMechMode())
            {
                BreakIntoResources();
            }
            else if (!Player.main.PlayBash())
            {
                Player.main.PlayGrab();
                FMODUWE.PlayOneShot(hitSound, base.transform.position);
                if ((bool)hitFX)
                {
                    Utils.PlayOneShotPS(hitFX, base.transform.position, Quaternion.Euler(new Vector3(270f, 0f, 0f)));
                }
                HitResource();
            }
        }

        public void HitResource()
        {
            hitsToBreak--;
            if (hitsToBreak == 0)
            {
                BreakIntoResources();
            }
        }

        private void BreakIntoResources()
        {
            SendMessage("OnBreakResource", null, SendMessageOptions.DontRequireReceiver);
            if ((bool)base.gameObject.GetComponent<VFXBurstModel>())
            {
                base.gameObject.BroadcastMessage("OnKill");
            }
            else
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
            if (customGoalText != "")
            {
                GoalManager.main.OnCustomGoalEvent(customGoalText);
            }
            bool flag = false;
            for (int i = 0; i < numChances; i++)
            {
                GameObject gameObject = ChooseRandomResource();
                if ((bool)gameObject)
                {
                    SpawnResourceFromPrefab(gameObject);
                    flag = true;
                }
            }
            if (!flag)
            {
                SpawnResourceFromPrefab(defaultPrefab);
            }
            FMODUWE.PlayOneShot(breakSound, base.transform.position);
            if ((bool)hitFX)
            {
                Utils.PlayOneShotPS(breakFX, base.transform.position, Quaternion.Euler(new Vector3(270f, 0f, 0f)));
            }
        }

        private void SpawnResourceFromPrefab(GameObject breakPrefab)
        {
            GameObject gameObject = global::UnityEngine.Object.Instantiate(breakPrefab, base.transform.position + base.transform.up * verticalSpawnOffset, Quaternion.identity);
            Debug.Log("broke, spawned " + breakPrefab.name);
            if (!gameObject.GetComponent<Rigidbody>())
            {
                gameObject.AddComponent<Rigidbody>();
            }
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
            gameObject.GetComponent<Rigidbody>().AddTorque(Vector3.right * global::UnityEngine.Random.Range(3, 6));
            gameObject.GetComponent<Rigidbody>().AddForce(base.transform.up * 0.1f);
        }

        private GameObject ChooseRandomResource()
        {
            GameObject result = null;
            for (int i = 0; i < prefabList.Count; i++)
            {
                RandomPrefab randomPrefab = prefabList[i];
                PlayerEntropy component = Player.main.gameObject.GetComponent<PlayerEntropy>();
                TechType techType = CraftData.GetTechType(randomPrefab.prefab);
                if (component.CheckChance(techType, randomPrefab.chance))
                {
                    result = randomPrefab.prefab;
                    break;
                }
            }
            return result;
        }
    }
}
