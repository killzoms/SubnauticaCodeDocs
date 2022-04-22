using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using AssemblyCSharp.Exploder;
using AssemblyCSharp.Exploder.MeshCutter;
using UnityEngine;
using Plane = AssemblyCSharp.Exploder.MeshCutter.Math.Plane;

namespace AssemblyCSharp
{
    public class ExploderObject : MonoBehaviour
    {
        [Serializable]
        public class SFXOption
        {
            public AudioClip ExplosionSoundClip;

            public AudioClip FragmentSoundClip;

            public GameObject FragmentEmitter;

            public float HitSoundTimeout;

            public int EmitersMax;

            public SFXOption Clone()
            {
                return new SFXOption
                {
                    ExplosionSoundClip = ExplosionSoundClip,
                    FragmentSoundClip = FragmentSoundClip,
                    FragmentEmitter = FragmentEmitter,
                    HitSoundTimeout = HitSoundTimeout,
                    EmitersMax = EmitersMax
                };
            }
        }

        [Serializable]
        public class FragmentOption
        {
            public bool FreezePositionX;

            public bool FreezePositionY;

            public bool FreezePositionZ;

            public bool FreezeRotationX;

            public bool FreezeRotationY;

            public bool FreezeRotationZ;

            public string Layer;

            public float MaxVelocity;

            public bool InheritParentPhysicsProperty;

            public float Mass;

            public bool UseGravity;

            public bool DisableColliders;

            public float AngularVelocity;

            public float MaxAngularVelocity;

            public Vector3 AngularVelocityVector;

            public bool RandomAngularVelocityVector;

            public FragmentOption Clone()
            {
                return new FragmentOption
                {
                    FreezePositionX = FreezePositionX,
                    FreezePositionY = FreezePositionY,
                    FreezePositionZ = FreezePositionZ,
                    FreezeRotationX = FreezeRotationX,
                    FreezeRotationY = FreezeRotationY,
                    FreezeRotationZ = FreezeRotationZ,
                    Layer = Layer,
                    Mass = Mass,
                    DisableColliders = DisableColliders,
                    UseGravity = UseGravity,
                    MaxVelocity = MaxVelocity,
                    MaxAngularVelocity = MaxAngularVelocity,
                    InheritParentPhysicsProperty = InheritParentPhysicsProperty,
                    AngularVelocity = AngularVelocity,
                    AngularVelocityVector = AngularVelocityVector,
                    RandomAngularVelocityVector = RandomAngularVelocityVector
                };
            }
        }

        public delegate void OnExplosion(float timeMS, ExplosionState state);

        public enum ExplosionState
        {
            ExplosionStarted,
            ExplosionFinished
        }

        public delegate void OnCracked();

        private enum State
        {
            None,
            Preprocess,
            ProcessCutter,
            IsolateMeshIslands,
            PostprocessInit,
            Postprocess,
            DryRun
        }

        private struct CutMesh
        {
            public Mesh mesh;

            public Material material;

            public Transform transform;

            public Transform parent;

            public Vector3 position;

            public Quaternion rotation;

            public Vector3 localScale;

            public GameObject original;

            public Vector3 centroid;

            public float distance;

            public int vertices;

            public int level;

            public int fragments;

            public ExploderOption option;

            public GameObject skinnedOriginal;
        }

        private struct MeshData
        {
            public Mesh sharedMesh;

            public Material sharedMaterial;

            public GameObject gameObject;

            public GameObject parentObject;

            public GameObject skinnedBakeOriginal;

            public Vector3 centroid;
        }

        public static ExploderObject main;

        public static string Tag = "Exploder";

        public bool DontUseTag;

        public float Radius = 10f;

        public Vector3 ForceVector = Vector3.up;

        public bool UseForceVector;

        public float Force = 30f;

        public float FrameBudget = 15f;

        public int TargetFragments = 30;

        public DeactivateOptions DeactivateOptions;

        public float DeactivateTimeout = 10f;

        public FadeoutOptions FadeoutOptions;

        public bool MeshColliders;

        public bool ExplodeSelf = true;

        public bool HideSelf = true;

        public bool DestroyOriginalObject;

        public bool ExplodeFragments = true;

        public bool UniformFragmentDistribution;

        public bool SplitMeshIslands;

        public bool AllowOpenMeshCutting;

        public GameObject currentExplodingObj;

        public int FragmentPoolSize = 200;

        public bool Use2DCollision;

        public SFXOption SFXOptions = new SFXOption
        {
            ExplosionSoundClip = null,
            FragmentSoundClip = null,
            FragmentEmitter = null,
            HitSoundTimeout = 0.3f,
            EmitersMax = 1000
        };

        public FragmentOption FragmentOptions = new FragmentOption
        {
            FreezePositionX = false,
            FreezePositionY = false,
            FreezePositionZ = false,
            FreezeRotationX = false,
            FreezeRotationY = false,
            FreezeRotationZ = false,
            Layer = "Default",
            Mass = 20f,
            MaxVelocity = 1000f,
            DisableColliders = false,
            UseGravity = true,
            InheritParentPhysicsProperty = true,
            AngularVelocity = 1f,
            AngularVelocityVector = Vector3.up,
            MaxAngularVelocity = 7f,
            RandomAngularVelocityVector = true
        };

        private OnExplosion ExplosionCallback;

        private OnCracked CrackedCallback;

        private bool crack;

        private bool cracked;

        private State state;

        private ExploderQueue queue;

        private MeshCutter cutter;

        private Stopwatch timer;

        private HashSet<CutMesh> newFragments;

        private HashSet<CutMesh> meshToRemove;

        private HashSet<CutMesh> meshSet;

        private int[] levelCount;

        private int poolIdx;

        private List<CutMesh> postList;

        private List<Fragment> pool;

        private Vector3 mainCentroid;

        private bool splitMeshIslands;

        private List<CutMesh> islands;

        private int explosionID;

        private AudioSource audioSource;

        public void Explode()
        {
            Explode(null);
        }

        public void Explode(OnExplosion callback)
        {
            queue.Explode(callback);
        }

        public void StartExplosionFromQueue(Vector3 pos, int id, OnExplosion callback)
        {
            mainCentroid = pos;
            explosionID = id;
            state = State.Preprocess;
            ExplosionCallback = callback;
        }

        public void Crack()
        {
            Crack(null);
        }

        public void Crack(OnCracked callback)
        {
            if (!crack)
            {
                CrackedCallback = callback;
                crack = true;
                cracked = false;
                Explode(null);
            }
        }

        public void ExplodeCracked(OnExplosion callback)
        {
            if (cracked)
            {
                PostCrackExplode(callback);
                crack = false;
            }
        }

        public void ExplodeCracked()
        {
            ExplodeCracked(null);
        }

        public void SetFromJavaScript(Hashtable hashtable)
        {
            foreach (object key in hashtable.Keys)
            {
                string text = (string)key;
                object obj = hashtable[key];
                switch (text)
                {
                    case "DontUseTag":
                        DontUseTag = (bool)obj;
                        break;
                    case "Radius":
                        Radius = (float)obj;
                        break;
                    case "ForceVector":
                        ForceVector = (Vector3)obj;
                        break;
                    case "Force":
                        Force = (float)obj;
                        break;
                    case "FrameBudget":
                        FrameBudget = (int)obj;
                        break;
                    case "TargetFragments":
                        TargetFragments = (int)obj;
                        break;
                    case "DeactivateOptions":
                        DeactivateOptions = (DeactivateOptions)obj;
                        break;
                    case "DeactivateTimeout":
                        DeactivateTimeout = (float)obj;
                        break;
                    case "MeshColliders":
                        MeshColliders = (bool)obj;
                        break;
                    case "ExplodeSelf":
                        ExplodeSelf = (bool)obj;
                        break;
                    case "HideSelf":
                        HideSelf = (bool)obj;
                        break;
                    case "DestroyOriginalObject":
                        DestroyOriginalObject = (bool)obj;
                        break;
                    case "ExplodeFragments":
                        ExplodeFragments = (bool)obj;
                        break;
                }
            }
        }

        public static void ExplodeGameObject(GameObject go)
        {
            go.AddComponent<Explodable>();
            main.currentExplodingObj = go;
            main.gameObject.SetActive(value: true);
            main.gameObject.transform.position = ExploderUtils.GetCentroid(go);
            main.Explode();
        }

        private void Awake()
        {
            if (main == null)
            {
                main = this;
            }
            cutter = new MeshCutter();
            cutter.Init(512, 512);
            global::UnityEngine.Random.seed = DateTime.Now.Millisecond;
            bool flag = false;
            flag = Use2DCollision;
            FragmentPool.Instance.Allocate(FragmentPoolSize, MeshColliders, flag);
            FragmentPool.Instance.SetDeactivateOptions(DeactivateOptions, FadeoutOptions, DeactivateTimeout);
            FragmentPool.Instance.SetExplodableFragments(ExplodeFragments, DontUseTag);
            FragmentPool.Instance.SetFragmentPhysicsOptions(FragmentOptions);
            FragmentPool.Instance.SetSFXOptions(SFXOptions);
            timer = new Stopwatch();
            queue = new ExploderQueue(this);
            if (DontUseTag)
            {
                base.gameObject.AddComponent<Explodable>();
            }
            else
            {
                base.gameObject.tag = "Exploder";
            }
            state = State.DryRun;
            PreAllocateBuffers();
            state = State.None;
            if ((bool)SFXOptions.ExplosionSoundClip)
            {
                audioSource = base.gameObject.GetComponent<AudioSource>();
                if (!audioSource)
                {
                    audioSource = base.gameObject.AddComponent<AudioSource>();
                }
            }
        }

        private void PreAllocateBuffers()
        {
            newFragments = new HashSet<CutMesh>();
            meshToRemove = new HashSet<CutMesh>();
            meshSet = new HashSet<CutMesh>();
            for (int i = 0; i < 64; i++)
            {
                meshSet.Add(default(CutMesh));
            }
            levelCount = new int[64];
            Preprocess();
            ProcessCutter(out var _);
        }

        private void OnDrawGizmos()
        {
            if (base.enabled)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(ExploderUtils.GetCentroid(base.gameObject), Radius);
            }
        }

        private int GetLevelFragments(int level, int fragmentsMax)
        {
            return fragmentsMax * 2 / (level * level + level) + 1;
        }

        private int GetLevel(float distance, float radius)
        {
            return (int)(distance / radius * 6f) / 2 + 1;
        }

        private List<MeshData> GetMeshData(GameObject obj)
        {
            MeshRenderer[] componentsInChildren = obj.GetComponentsInChildren<MeshRenderer>();
            MeshFilter[] componentsInChildren2 = obj.GetComponentsInChildren<MeshFilter>();
            if (componentsInChildren.Length != componentsInChildren2.Length)
            {
                return new List<MeshData>();
            }
            List<MeshData> list = new List<MeshData>(componentsInChildren.Length);
            MeshData item;
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                if (!(componentsInChildren2[i].sharedMesh == null))
                {
                    if (!componentsInChildren2[i].sharedMesh || !componentsInChildren2[i].sharedMesh.isReadable)
                    {
                        global::UnityEngine.Debug.LogWarning("Mesh is not readable: " + componentsInChildren2[i].name);
                        continue;
                    }
                    item = new MeshData
                    {
                        sharedMesh = componentsInChildren2[i].sharedMesh,
                        sharedMaterial = componentsInChildren[i].sharedMaterial,
                        gameObject = componentsInChildren[i].gameObject,
                        centroid = componentsInChildren[i].bounds.center,
                        parentObject = obj
                    };
                    list.Add(item);
                }
            }
            SkinnedMeshRenderer[] componentsInChildren3 = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int j = 0; j < componentsInChildren3.Length; j++)
            {
                if (!componentsInChildren3[j].sharedMesh || !componentsInChildren3[j].sharedMesh.isReadable)
                {
                    global::UnityEngine.Debug.LogWarning("Mesh is not readable: " + componentsInChildren3[j].name);
                    continue;
                }
                Mesh mesh = new Mesh();
                componentsInChildren3[j].BakeMesh(mesh);
                GameObject gameObject = new GameObject("BakeSkin");
                gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = componentsInChildren3[j].material;
                gameObject.transform.position = obj.transform.position;
                gameObject.transform.rotation = obj.transform.rotation;
                ExploderUtils.SetVisible(gameObject, status: false);
                item = new MeshData
                {
                    sharedMesh = mesh,
                    sharedMaterial = meshRenderer.sharedMaterial,
                    gameObject = gameObject,
                    centroid = meshRenderer.bounds.center,
                    parentObject = gameObject,
                    skinnedBakeOriginal = obj
                };
                list.Add(item);
            }
            return list;
        }

        private bool IsExplodable(GameObject obj)
        {
            if (DontUseTag)
            {
                return obj.GetComponent<Explodable>() != null;
            }
            return obj.CompareTag(Tag);
        }

        private List<CutMesh> GetMeshList()
        {
            GameObject[] array = null;
            if (DontUseTag)
            {
                global::UnityEngine.Object[] array2 = global::UnityEngine.Object.FindObjectsOfType(typeof(Explodable));
                List<GameObject> list = new List<GameObject>(array2.Length);
                global::UnityEngine.Object[] array3 = array2;
                for (int i = 0; i < array3.Length; i++)
                {
                    Explodable explodable = (Explodable)array3[i];
                    if ((bool)explodable)
                    {
                        list.Add(explodable.gameObject);
                    }
                }
                array = list.ToArray();
            }
            else
            {
                array = GameObject.FindGameObjectsWithTag("Exploder");
            }
            List<CutMesh> list2 = new List<CutMesh>(array.Length);
            GameObject[] array4 = array;
            foreach (GameObject gameObject in array4)
            {
                if ((ExplodeSelf || !(gameObject == base.gameObject)) && (ExploderUtils.GetCentroid(gameObject) - mainCentroid).sqrMagnitude < Radius * Radius)
                {
                    List<MeshData> meshData = GetMeshData(gameObject);
                    int count = meshData.Count;
                    for (int j = 0; j < count; j++)
                    {
                        Vector3 centroid = meshData[j].centroid;
                        float magnitude = (centroid - mainCentroid).magnitude;
                        list2.Add(new CutMesh
                        {
                            mesh = meshData[j].sharedMesh,
                            material = meshData[j].sharedMaterial,
                            centroid = meshData[j].gameObject.transform.InverseTransformPoint(centroid),
                            vertices = meshData[j].sharedMesh.vertexCount,
                            transform = meshData[j].gameObject.transform,
                            parent = meshData[j].gameObject.transform.parent,
                            position = meshData[j].gameObject.transform.position,
                            rotation = meshData[j].gameObject.transform.rotation,
                            localScale = meshData[j].gameObject.transform.localScale,
                            distance = magnitude,
                            level = GetLevel(magnitude, Radius),
                            original = meshData[j].parentObject,
                            skinnedOriginal = meshData[j].skinnedBakeOriginal,
                            option = gameObject.GetComponent<ExploderOption>()
                        });
                    }
                }
            }
            if (list2.Count == 0)
            {
                return list2;
            }
            list2.Sort((CutMesh m0, CutMesh m1) => m0.level.CompareTo(m1.level));
            if (list2.Count > TargetFragments)
            {
                list2.RemoveRange(TargetFragments - 1, list2.Count - TargetFragments);
            }
            int level = list2[list2.Count - 1].level;
            int levelFragments = GetLevelFragments(level, TargetFragments);
            int num = 0;
            int count2 = list2.Count;
            int[] array5 = new int[level + 1];
            foreach (CutMesh item in list2)
            {
                array5[item.level]++;
            }
            for (int k = 0; k < count2; k++)
            {
                CutMesh value = list2[k];
                num += (value.fragments = (level + 1 - value.level) * levelFragments / array5[value.level]);
                list2[k] = value;
                if (num >= TargetFragments)
                {
                    value.fragments -= num - TargetFragments;
                    num -= num - TargetFragments;
                    list2[k] = value;
                    break;
                }
            }
            return list2;
        }

        private void Update()
        {
            long cuttingTime = 0L;
            switch (state)
            {
                case State.Preprocess:
                    timer.Reset();
                    timer.Start();
                    if (!Preprocess())
                    {
                        OnExplosionFinished(success: false);
                        break;
                    }
                    state = State.ProcessCutter;
                    goto case State.ProcessCutter;
                case State.ProcessCutter:
                    if (ProcessCutter(out cuttingTime))
                    {
                        poolIdx = 0;
                        postList = new List<CutMesh>(meshSet);
                        if (splitMeshIslands)
                        {
                            islands = new List<CutMesh>(meshSet.Count);
                            state = State.IsolateMeshIslands;
                            goto case State.IsolateMeshIslands;
                        }
                        state = State.PostprocessInit;
                        goto case State.PostprocessInit;
                    }
                    break;
                case State.IsolateMeshIslands:
                    if (IsolateMeshIslands(ref cuttingTime))
                    {
                        state = State.PostprocessInit;
                        goto case State.PostprocessInit;
                    }
                    break;
                case State.PostprocessInit:
                    InitPostprocess();
                    state = State.Postprocess;
                    goto case State.Postprocess;
                case State.Postprocess:
                    if (Postprocess(cuttingTime))
                    {
                        timer.Stop();
                    }
                    break;
                case State.None:
                    break;
            }
        }

        private bool Preprocess()
        {
            List<CutMesh> meshList = GetMeshList();
            if (meshList.Count == 0)
            {
                return false;
            }
            newFragments.Clear();
            meshToRemove.Clear();
            meshSet = new HashSet<CutMesh>(meshList);
            splitMeshIslands = SplitMeshIslands;
            int level = meshList[meshList.Count - 1].level;
            levelCount = new int[level + 1];
            foreach (CutMesh item in meshSet)
            {
                levelCount[item.level] += item.fragments;
            }
            if (UniformFragmentDistribution)
            {
                int[] array = new int[64];
                foreach (CutMesh item2 in meshSet)
                {
                    array[item2.level]++;
                }
                int num = TargetFragments / meshSet.Count;
                foreach (CutMesh item3 in meshSet)
                {
                    levelCount[item3.level] = num * array[item3.level];
                }
            }
            return true;
        }

        private bool ProcessCutter(out long cuttingTime)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool flag = true;
            bool flag2 = false;
            int num = 0;
            while (flag)
            {
                num++;
                if (num > TargetFragments)
                {
                    break;
                }
                int count = meshSet.Count;
                newFragments.Clear();
                meshToRemove.Clear();
                flag = false;
                foreach (CutMesh item in meshSet)
                {
                    if (levelCount[item.level] <= 0)
                    {
                        continue;
                    }
                    Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
                    if (!item.transform)
                    {
                        continue;
                    }
                    Plane plane = new Plane(insideUnitSphere, item.transform.TransformPoint(item.centroid));
                    bool triangulateHoles = true;
                    Color crossSectionVertexColor = Color.white;
                    Vector4 crossUV = new Vector4(0f, 0f, 1f, 1f);
                    if ((bool)item.option)
                    {
                        triangulateHoles = !item.option.Plane2D;
                        crossSectionVertexColor = item.option.CrossSectionVertexColor;
                        crossUV = item.option.CrossSectionUV;
                        splitMeshIslands |= item.option.SplitMeshIslands;
                    }
                    if (Use2DCollision)
                    {
                        triangulateHoles = false;
                    }
                    List<CutterMesh> meshes = null;
                    cutter.Cut(item.mesh, item.transform, plane, triangulateHoles, AllowOpenMeshCutting, ref meshes, crossSectionVertexColor, crossUV);
                    flag = true;
                    if (meshes == null)
                    {
                        continue;
                    }
                    foreach (CutterMesh item2 in meshes)
                    {
                        newFragments.Add(new CutMesh
                        {
                            mesh = item2.mesh,
                            centroid = item2.centroid,
                            material = item.material,
                            vertices = item.vertices,
                            transform = item.transform,
                            distance = item.distance,
                            level = item.level,
                            fragments = item.fragments,
                            original = item.original,
                            skinnedOriginal = item.skinnedOriginal,
                            parent = item.transform.parent,
                            position = item.transform.position,
                            rotation = item.transform.rotation,
                            localScale = item.transform.localScale,
                            option = item.option
                        });
                    }
                    meshToRemove.Add(item);
                    levelCount[item.level]--;
                    if (count + newFragments.Count - meshToRemove.Count >= TargetFragments)
                    {
                        cuttingTime = stopwatch.ElapsedMilliseconds;
                        return true;
                    }
                    if ((float)stopwatch.ElapsedMilliseconds > FrameBudget)
                    {
                        flag2 = true;
                        break;
                    }
                }
                meshSet.ExceptWith(meshToRemove);
                meshSet.UnionWith(newFragments);
                if (flag2)
                {
                    break;
                }
            }
            cuttingTime = stopwatch.ElapsedMilliseconds;
            if (!flag2)
            {
                return true;
            }
            return false;
        }

        private bool IsolateMeshIslands(ref long timeOffset)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = postList.Count;
            while (poolIdx < count)
            {
                CutMesh item = postList[poolIdx];
                poolIdx++;
                bool flag = false;
                if (SplitMeshIslands || ((bool)item.option && item.option.SplitMeshIslands))
                {
                    List<CutterMesh> list = MeshUtils.IsolateMeshIslands(item.mesh);
                    if (list != null)
                    {
                        flag = true;
                        foreach (CutterMesh item2 in list)
                        {
                            islands.Add(new CutMesh
                            {
                                mesh = item2.mesh,
                                centroid = item2.centroid,
                                material = item.material,
                                vertices = item.vertices,
                                transform = item.transform,
                                distance = item.distance,
                                level = item.level,
                                fragments = item.fragments,
                                original = item.original,
                                skinnedOriginal = item.skinnedOriginal,
                                parent = item.transform.parent,
                                position = item.transform.position,
                                rotation = item.transform.rotation,
                                localScale = item.transform.localScale,
                                option = item.option
                            });
                        }
                    }
                }
                if (!flag)
                {
                    islands.Add(item);
                }
                if ((float)(stopwatch.ElapsedMilliseconds + timeOffset) > FrameBudget)
                {
                    return false;
                }
            }
            postList = islands;
            return true;
        }

        private void InitPostprocess()
        {
            int count = postList.Count;
            bool flag = false;
            flag = Use2DCollision;
            FragmentPool.Instance.Allocate(count, MeshColliders, flag);
            FragmentPool.Instance.SetDeactivateOptions(DeactivateOptions, FadeoutOptions, DeactivateTimeout);
            FragmentPool.Instance.SetExplodableFragments(ExplodeFragments, DontUseTag);
            FragmentPool.Instance.SetFragmentPhysicsOptions(FragmentOptions);
            FragmentPool.Instance.SetSFXOptions(SFXOptions);
            poolIdx = 0;
            pool = FragmentPool.Instance.GetAvailableFragments(count);
            if (ExplosionCallback != null)
            {
                ExplosionCallback(timer.ElapsedMilliseconds, ExplosionState.ExplosionStarted);
            }
            if ((bool)SFXOptions.ExplosionSoundClip)
            {
                if (!audioSource)
                {
                    audioSource = base.gameObject.AddComponent<AudioSource>();
                }
                audioSource.PlayOneShot(SFXOptions.ExplosionSoundClip);
            }
        }

        private void PostCrackExplode(OnExplosion callback)
        {
            callback?.Invoke(0f, ExplosionState.ExplosionStarted);
            int count = postList.Count;
            poolIdx = 0;
            while (poolIdx < count)
            {
                Fragment fragment = pool[poolIdx];
                CutMesh cutMesh = postList[poolIdx];
                poolIdx++;
                if (cutMesh.original != base.gameObject)
                {
                    ExploderUtils.SetActiveRecursively(cutMesh.original, status: false);
                }
                else
                {
                    ExploderUtils.EnableCollider(cutMesh.original, status: false);
                    ExploderUtils.SetVisible(cutMesh.original, status: false);
                }
                if ((bool)cutMesh.skinnedOriginal && cutMesh.skinnedOriginal != base.gameObject)
                {
                    ExploderUtils.SetActiveRecursively(cutMesh.skinnedOriginal, status: false);
                }
                else
                {
                    ExploderUtils.EnableCollider(cutMesh.skinnedOriginal, status: false);
                    ExploderUtils.SetVisible(cutMesh.skinnedOriginal, status: false);
                }
                fragment.Explode();
            }
            if (DestroyOriginalObject)
            {
                foreach (CutMesh post in postList)
                {
                    if ((bool)post.original && !post.original.GetComponent<Fragment>())
                    {
                        global::UnityEngine.Object.Destroy(post.original);
                    }
                    if ((bool)post.skinnedOriginal)
                    {
                        global::UnityEngine.Object.Destroy(post.skinnedOriginal);
                    }
                }
            }
            if (ExplodeSelf && !DestroyOriginalObject)
            {
                ExploderUtils.SetActiveRecursively(base.gameObject, status: false);
            }
            if (HideSelf)
            {
                ExploderUtils.SetActiveRecursively(base.gameObject, status: false);
            }
            ExplosionCallback = callback;
            OnExplosionFinished(success: true);
        }

        private bool Postprocess(long timeOffset)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = postList.Count;
            while (poolIdx < count)
            {
                Fragment fragment = pool[poolIdx];
                CutMesh cutMesh = postList[poolIdx];
                poolIdx++;
                if (!cutMesh.original)
                {
                    continue;
                }
                if (crack)
                {
                    ExploderUtils.SetActiveRecursively(fragment.gameObject, status: false);
                }
                fragment.meshFilter.sharedMesh = cutMesh.mesh;
                fragment.meshRenderer.sharedMaterial = cutMesh.material;
                cutMesh.mesh.RecalculateBounds();
                Transform parent = fragment.transform.parent;
                fragment.transform.parent = cutMesh.parent;
                fragment.transform.position = cutMesh.position;
                fragment.transform.rotation = cutMesh.rotation;
                fragment.transform.localScale = cutMesh.localScale;
                fragment.transform.parent = null;
                fragment.transform.parent = parent;
                if (!crack)
                {
                    if (cutMesh.original != base.gameObject)
                    {
                        ExploderUtils.SetActiveRecursively(cutMesh.original, status: false);
                    }
                    else
                    {
                        ExploderUtils.EnableCollider(cutMesh.original, status: false);
                        ExploderUtils.SetVisible(cutMesh.original, status: false);
                    }
                    if ((bool)cutMesh.skinnedOriginal && cutMesh.skinnedOriginal != base.gameObject)
                    {
                        ExploderUtils.SetActiveRecursively(cutMesh.skinnedOriginal, status: false);
                    }
                    else
                    {
                        ExploderUtils.EnableCollider(cutMesh.skinnedOriginal, status: false);
                        ExploderUtils.SetVisible(cutMesh.skinnedOriginal, status: false);
                    }
                }
                bool flag = (bool)cutMesh.option && cutMesh.option.Plane2D;
                bool flag2 = false;
                flag2 = Use2DCollision;
                if (!FragmentOptions.DisableColliders)
                {
                    if (MeshColliders && !flag2)
                    {
                        if (!flag)
                        {
                            fragment.meshCollider.sharedMesh = cutMesh.mesh;
                        }
                    }
                    else if (Use2DCollision)
                    {
                        MeshUtils.GeneratePolygonCollider(fragment.polygonCollider2D, cutMesh.mesh);
                    }
                    else
                    {
                        fragment.boxCollider.center = cutMesh.mesh.bounds.center;
                        fragment.boxCollider.size = cutMesh.mesh.bounds.extents;
                    }
                }
                if ((bool)cutMesh.option)
                {
                    cutMesh.option.DuplicateSettings(fragment.options);
                }
                if (!crack)
                {
                    fragment.Explode();
                    fragment.gameObject.name = main.currentExplodingObj.name + "_" + fragment.gameObject.name;
                    CraftData.ProcessFragment(main.currentExplodingObj, fragment.gameObject);
                }
                float force = Force;
                if ((bool)cutMesh.option && cutMesh.option.UseLocalForce)
                {
                    force = cutMesh.option.Force;
                }
                fragment.ApplyExplosion(cutMesh.transform, cutMesh.centroid, mainCentroid, FragmentOptions, UseForceVector, ForceVector, force, cutMesh.original, TargetFragments);
                if ((float)(stopwatch.ElapsedMilliseconds + timeOffset) > FrameBudget)
                {
                    return false;
                }
            }
            if (!crack)
            {
                if (DestroyOriginalObject)
                {
                    foreach (CutMesh post in postList)
                    {
                        if ((bool)post.original && !post.original.GetComponent<Fragment>())
                        {
                            global::UnityEngine.Object.Destroy(post.original);
                        }
                        if ((bool)post.skinnedOriginal)
                        {
                            global::UnityEngine.Object.Destroy(post.skinnedOriginal);
                        }
                    }
                }
                if (ExplodeSelf && !DestroyOriginalObject)
                {
                    ExploderUtils.SetActiveRecursively(base.gameObject, status: false);
                }
                if (HideSelf)
                {
                    ExploderUtils.SetActiveRecursively(base.gameObject, status: false);
                }
                OnExplosionFinished(success: true);
            }
            else
            {
                cracked = true;
                if (CrackedCallback != null)
                {
                    CrackedCallback();
                }
            }
            return true;
        }

        private void OnExplosionFinished(bool success)
        {
            if (ExplosionCallback != null)
            {
                if (!success)
                {
                    ExplosionCallback(timer.ElapsedMilliseconds, ExplosionState.ExplosionStarted);
                    OnExplosionStarted();
                }
                ExplosionCallback(timer.ElapsedMilliseconds, ExplosionState.ExplosionFinished);
            }
            state = State.None;
            queue.OnExplosionFinished(explosionID);
        }

        private void OnExplosionStarted()
        {
        }
    }
}
