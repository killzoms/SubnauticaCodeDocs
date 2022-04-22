using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp.Exploder
{
    public class FragmentPool : MonoBehaviour
    {
        private static FragmentPool instance;

        private Fragment[] pool;

        private bool meshColliders;

        private float fragmentSoundTimeout;

        public float HitSoundTimeout = 1f;

        public int MaxEmitters = 1000;

        public static FragmentPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("FragmentRoot").AddComponent<FragmentPool>();
                }
                return instance;
            }
        }

        public int PoolSize => pool.Length;

        public Fragment[] Pool => pool;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            DestroyFragments();
            instance = null;
        }

        public List<Fragment> GetAvailableFragments(int size)
        {
            if (size > pool.Length)
            {
                Debug.LogError("Requesting pool size higher than allocated! Please call Allocate first! " + size);
                return null;
            }
            if (size == pool.Length)
            {
                return new List<Fragment>(pool);
            }
            List<Fragment> list = new List<Fragment>();
            int num = 0;
            Fragment[] array = pool;
            foreach (Fragment fragment in array)
            {
                if ((bool)fragment && !fragment.activeObj)
                {
                    list.Add(fragment);
                    num++;
                }
                if (num == size)
                {
                    return list;
                }
            }
            array = pool;
            foreach (Fragment fragment2 in array)
            {
                if ((bool)fragment2 && !fragment2.visible)
                {
                    list.Add(fragment2);
                    num++;
                }
                if (num == size)
                {
                    return list;
                }
            }
            if (num < size)
            {
                array = pool;
                foreach (Fragment fragment3 in array)
                {
                    if ((bool)fragment3 && fragment3.IsSleeping() && fragment3.visible)
                    {
                        list.Add(fragment3);
                        num++;
                    }
                    if (num == size)
                    {
                        return list;
                    }
                }
            }
            if (num < size)
            {
                array = pool;
                foreach (Fragment fragment4 in array)
                {
                    if ((bool)fragment4 && !fragment4.IsSleeping() && fragment4.visible)
                    {
                        list.Add(fragment4);
                        num++;
                    }
                    if (num == size)
                    {
                        return list;
                    }
                }
            }
            return null;
        }

        public void Allocate(int poolSize, bool useMeshColliders, bool use2dCollision)
        {
            if (pool != null && pool.Length >= poolSize && useMeshColliders == meshColliders && Array.IndexOf(pool, null) == -1)
            {
                return;
            }
            DestroyFragments();
            pool = new Fragment[poolSize];
            meshColliders = useMeshColliders;
            for (int i = 0; i < poolSize; i++)
            {
                GameObject gameObject = new GameObject("fragment_" + i);
                gameObject.AddComponent<MeshFilter>();
                gameObject.AddComponent<MeshRenderer>();
                if (use2dCollision)
                {
                    gameObject.AddComponent<PolygonCollider2D>();
                    gameObject.AddComponent<Rigidbody2D>();
                }
                else
                {
                    if (useMeshColliders)
                    {
                        gameObject.AddComponent<MeshCollider>().convex = true;
                    }
                    else
                    {
                        gameObject.AddComponent<BoxCollider>();
                    }
                    gameObject.AddComponent<Rigidbody>();
                }
                gameObject.AddComponent<ExploderOption>();
                Fragment fragment = gameObject.AddComponent<Fragment>();
                gameObject.transform.parent = base.gameObject.transform;
                pool[i] = fragment;
                ExploderUtils.SetActiveRecursively(gameObject.gameObject, status: false);
                fragment.RefreshComponentsCache();
                fragment.Sleep();
            }
        }

        public void WakeUp()
        {
            Fragment[] array = pool;
            foreach (Fragment fragment in array)
            {
                if ((bool)fragment)
                {
                    fragment.WakeUp();
                }
            }
        }

        public void Sleep()
        {
            Fragment[] array = pool;
            foreach (Fragment fragment in array)
            {
                if ((bool)fragment)
                {
                    fragment.Sleep();
                }
            }
        }

        public void DestroyFragments()
        {
            if (pool == null)
            {
                return;
            }
            Fragment[] array = pool;
            foreach (Fragment fragment in array)
            {
                if ((bool)fragment)
                {
                    global::UnityEngine.Object.Destroy(fragment.gameObject);
                }
            }
            pool = null;
        }

        public void DeactivateFragments()
        {
            if (pool == null)
            {
                return;
            }
            Fragment[] array = pool;
            foreach (Fragment fragment in array)
            {
                if ((bool)fragment)
                {
                    fragment.Deactivate();
                }
            }
        }

        public void SetDeactivateOptions(DeactivateOptions options, FadeoutOptions fadeoutOptions, float timeout)
        {
            if (pool == null)
            {
                return;
            }
            Fragment[] array = pool;
            foreach (Fragment fragment in array)
            {
                if ((bool)fragment)
                {
                    fragment.deactivateOptions = options;
                    fragment.deactivateTimeout = timeout;
                    fragment.fadeoutOptions = fadeoutOptions;
                }
            }
        }

        public void SetExplodableFragments(bool explodable, bool dontUseTag)
        {
            if (!(pool != null && explodable))
            {
                return;
            }
            if (dontUseTag)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    Fragment fragment = pool[i];
                    if ((bool)fragment && (bool)fragment.gameObject)
                    {
                        fragment.gameObject.AddComponent<Explodable>();
                    }
                }
                return;
            }
            for (int j = 0; j < pool.Length; j++)
            {
                Fragment fragment2 = pool[j];
                if ((bool)fragment2)
                {
                    fragment2.tag = ExploderObject.Tag;
                }
            }
        }

        public void SetFragmentPhysicsOptions(ExploderObject.FragmentOption options)
        {
            if (pool == null)
            {
                return;
            }
            RigidbodyConstraints rigidbodyConstraints = RigidbodyConstraints.None;
            if (options.FreezePositionX)
            {
                rigidbodyConstraints |= RigidbodyConstraints.FreezePositionX;
            }
            if (options.FreezePositionY)
            {
                rigidbodyConstraints |= RigidbodyConstraints.FreezePositionY;
            }
            if (options.FreezePositionZ)
            {
                rigidbodyConstraints |= RigidbodyConstraints.FreezePositionZ;
            }
            if (options.FreezeRotationX)
            {
                rigidbodyConstraints |= RigidbodyConstraints.FreezeRotationX;
            }
            if (options.FreezeRotationY)
            {
                rigidbodyConstraints |= RigidbodyConstraints.FreezeRotationY;
            }
            if (options.FreezeRotationZ)
            {
                rigidbodyConstraints |= RigidbodyConstraints.FreezeRotationZ;
            }
            Fragment[] array = pool;
            foreach (Fragment fragment in array)
            {
                if ((bool)fragment && (bool)fragment.gameObject)
                {
                    int layer = 0;
                    fragment.gameObject.layer = layer;
                    fragment.SetConstraints(rigidbodyConstraints);
                    fragment.DisableColliders(options.DisableColliders, meshColliders);
                }
            }
        }

        public void SetSFXOptions(ExploderObject.SFXOption sfx)
        {
            if (pool == null)
            {
                return;
            }
            HitSoundTimeout = sfx.HitSoundTimeout;
            MaxEmitters = sfx.EmitersMax;
            for (int i = 0; i < pool.Length; i++)
            {
                Fragment fragment = pool[i];
                if ((bool)fragment)
                {
                    fragment.SetSFX(sfx, i < MaxEmitters);
                }
            }
        }

        public List<Fragment> GetActiveFragments()
        {
            if (pool != null)
            {
                List<Fragment> list = new List<Fragment>(pool.Length);
                Fragment[] array = pool;
                foreach (Fragment fragment in array)
                {
                    if ((bool)fragment && ExploderUtils.IsActive(fragment.gameObject))
                    {
                        list.Add(fragment);
                    }
                }
                return list;
            }
            return null;
        }

        private void Update()
        {
            fragmentSoundTimeout -= Time.deltaTime;
        }

        public void OnFragmentHit()
        {
            fragmentSoundTimeout = HitSoundTimeout;
        }

        public bool CanPlayHitSound()
        {
            return fragmentSoundTimeout <= 0f;
        }
    }
}
