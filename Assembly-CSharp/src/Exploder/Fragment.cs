using UnityEngine;

namespace AssemblyCSharp.Exploder
{
    public class Fragment : MonoBehaviour
    {
        public bool explodable;

        public DeactivateOptions deactivateOptions;

        public float deactivateTimeout = 10f;

        public FadeoutOptions fadeoutOptions;

        public float maxVelocity = 1000f;

        public bool disableColliders;

        public float disableCollidersTimeout;

        public bool visible;

        public bool activeObj;

        public float minSizeToExplode = 0.5f;

        public MeshFilter meshFilter;

        public MeshRenderer meshRenderer;

        public MeshCollider meshCollider;

        public BoxCollider boxCollider;

        public AudioSource audioSource;

        public AudioClip audioClip;

        private GameObject particleChild;

        public PolygonCollider2D polygonCollider2D;

        public Rigidbody2D rigid2D;

        public ExploderOption options;

        public Rigidbody rigidBody;

        private Vector3 originalScale;

        private float visibilityCheckTimer;

        private float deactivateTimer;

        public bool IsSleeping()
        {
            if ((bool)rigid2D)
            {
                return rigid2D.IsSleeping();
            }
            return rigidBody.IsSleeping();
        }

        public void Sleep()
        {
            if ((bool)rigid2D)
            {
                rigid2D.Sleep();
            }
            else
            {
                rigidBody.Sleep();
            }
        }

        public void WakeUp()
        {
            if ((bool)rigid2D)
            {
                rigid2D.WakeUp();
            }
            else
            {
                rigidBody.WakeUp();
            }
        }

        public void SetConstraints(RigidbodyConstraints constraints)
        {
            if ((bool)GetComponent<Rigidbody>())
            {
                rigidBody.constraints = constraints;
            }
        }

        public void SetSFX(ExploderObject.SFXOption sfx, bool allowParticle)
        {
            audioClip = sfx.FragmentSoundClip;
            if ((bool)audioClip && !audioSource)
            {
                audioSource = base.gameObject.AddComponent<AudioSource>();
            }
            if ((bool)sfx.FragmentEmitter && allowParticle)
            {
                if (!particleChild)
                {
                    GameObject gameObject = Object.Instantiate(sfx.FragmentEmitter);
                    if ((bool)gameObject)
                    {
                        particleChild = new GameObject("Particles");
                        particleChild.transform.parent = base.gameObject.transform;
                        gameObject.transform.parent = particleChild.transform;
                    }
                }
            }
            else if ((bool)particleChild)
            {
                Object.Destroy(particleChild);
            }
        }

        private void OnCollisionEnter()
        {
            FragmentPool instance = FragmentPool.Instance;
            if (instance.CanPlayHitSound())
            {
                if ((bool)audioClip && (bool)audioSource)
                {
                    audioSource.PlayOneShot(audioClip);
                }
                instance.OnFragmentHit();
            }
        }

        public void DisableColliders(bool disable, bool meshColliders)
        {
            if (disable)
            {
                if ((bool)meshCollider)
                {
                    Object.Destroy(meshCollider);
                }
                if ((bool)boxCollider)
                {
                    Object.Destroy(boxCollider);
                }
            }
            else if (meshColliders)
            {
                if (!meshCollider)
                {
                    meshCollider = base.gameObject.AddComponent<MeshCollider>();
                }
            }
            else if (!boxCollider)
            {
                boxCollider = base.gameObject.AddComponent<BoxCollider>();
            }
        }

        public void ApplyExplosion(Transform meshTransform, Vector3 centroid, Vector3 mainCentroid, ExploderObject.FragmentOption fragmentOption, bool useForceVector, Vector3 ForceVector, float force, GameObject original, int targetFragments)
        {
            if ((bool)rigid2D)
            {
                ApplyExplosion2D(meshTransform, centroid, mainCentroid, fragmentOption, useForceVector, ForceVector, force, original, targetFragments);
                return;
            }
            Rigidbody rigidbody = rigidBody;
            Vector3 vector = Vector3.zero;
            Vector3 vector2 = Vector3.zero;
            float mass = fragmentOption.Mass;
            bool useGravity = fragmentOption.UseGravity;
            rigidbody.maxAngularVelocity = fragmentOption.MaxAngularVelocity;
            if (fragmentOption.InheritParentPhysicsProperty && (bool)original && (bool)original.GetComponent<Rigidbody>())
            {
                Rigidbody component = original.GetComponent<Rigidbody>();
                vector = component.velocity;
                vector2 = component.angularVelocity;
                mass = component.mass / (float)targetFragments;
                useGravity = component.useGravity;
            }
            Vector3 vector3 = (meshTransform.TransformPoint(centroid) - mainCentroid).normalized;
            Vector3 vector4 = fragmentOption.AngularVelocity * (fragmentOption.RandomAngularVelocityVector ? Random.onUnitSphere : fragmentOption.AngularVelocityVector);
            if (useForceVector)
            {
                vector3 = ForceVector;
            }
            rigidbody.velocity = vector3 * force + vector;
            rigidbody.angularVelocity = vector4 + vector2;
            rigidbody.mass = mass;
            maxVelocity = fragmentOption.MaxVelocity;
            rigidbody.useGravity = useGravity;
        }

        private void ApplyExplosion2D(Transform meshTransform, Vector3 centroid, Vector3 mainCentroid, ExploderObject.FragmentOption fragmentOption, bool useForceVector, Vector2 ForceVector, float force, GameObject original, int targetFragments)
        {
            Rigidbody2D rigidbody2D = rigid2D;
            Vector2 vector = Vector2.zero;
            float num = 0f;
            float mass = fragmentOption.Mass;
            if (fragmentOption.InheritParentPhysicsProperty && (bool)original && (bool)original.GetComponent<Rigidbody2D>())
            {
                Rigidbody2D component = original.GetComponent<Rigidbody2D>();
                vector = component.velocity;
                num = component.angularVelocity;
                mass = component.mass / (float)targetFragments;
            }
            Vector2 vector2 = (meshTransform.TransformPoint(centroid) - mainCentroid).normalized;
            float num2 = fragmentOption.AngularVelocity * (fragmentOption.RandomAngularVelocityVector ? Random.insideUnitCircle.x : fragmentOption.AngularVelocityVector.y);
            if (useForceVector)
            {
                vector2 = ForceVector;
            }
            rigidbody2D.velocity = vector2 * force + vector;
            rigidbody2D.angularVelocity = num2 + num;
            rigidbody2D.mass = mass;
            maxVelocity = fragmentOption.MaxVelocity;
        }

        public void RefreshComponentsCache()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            boxCollider = GetComponent<BoxCollider>();
            options = GetComponent<ExploderOption>();
            rigidBody = GetComponent<Rigidbody>();
            rigid2D = GetComponent<Rigidbody2D>();
            polygonCollider2D = GetComponent<PolygonCollider2D>();
        }

        public void Explode()
        {
            activeObj = true;
            ExploderUtils.SetActiveRecursively(base.gameObject, status: true);
            visibilityCheckTimer = 0.1f;
            visible = true;
            deactivateTimer = deactivateTimeout;
            originalScale = base.transform.localScale;
            if (explodable)
            {
                base.tag = ExploderObject.Tag;
            }
            Emit();
        }

        public void Emit()
        {
        }

        public void Deactivate()
        {
            ExploderUtils.SetActive(base.gameObject, status: false);
            visible = false;
            activeObj = false;
        }

        private void Start()
        {
            visibilityCheckTimer = 1f;
            RefreshComponentsCache();
            visible = false;
        }

        private void Update()
        {
            if (!activeObj)
            {
                return;
            }
            if (rigidBody.velocity.sqrMagnitude > maxVelocity * maxVelocity)
            {
                Vector3 normalized = rigidBody.velocity.normalized;
                rigidBody.velocity = normalized * maxVelocity;
            }
            if (deactivateOptions == DeactivateOptions.Timeout)
            {
                deactivateTimer -= Time.deltaTime;
                if (deactivateTimer < 0f)
                {
                    Sleep();
                    activeObj = false;
                    ExploderUtils.SetActiveRecursively(base.gameObject, status: false);
                    FadeoutOptions fadeoutOptions = this.fadeoutOptions;
                    if (fadeoutOptions == FadeoutOptions.FadeoutAlpha)
                    {
                    }
                }
                else
                {
                    float num = deactivateTimer / deactivateTimeout;
                    switch (this.fadeoutOptions)
                    {
                    case FadeoutOptions.FadeoutAlpha:
                        if ((bool)meshRenderer.material && meshRenderer.material.HasProperty("_Color"))
                        {
                            Color color = meshRenderer.material.color;
                            color.a = num;
                            meshRenderer.material.color = color;
                        }
                        break;
                    case FadeoutOptions.ScaleDown:
                        base.gameObject.transform.localScale = originalScale * num;
                        break;
                    }
                }
            }
            visibilityCheckTimer -= Time.deltaTime;
            if (!(visibilityCheckTimer < 0f) || !MainCamera.camera)
            {
                return;
            }
            Vector3 vector = MainCamera.camera.WorldToViewportPoint(base.transform.position);
            if (vector.z < 0f || vector.x < 0f || vector.y < 0f || vector.x > 1f || vector.y > 1f)
            {
                if (deactivateOptions == DeactivateOptions.OutsideOfCamera)
                {
                    Sleep();
                    activeObj = false;
                    ExploderUtils.SetActiveRecursively(base.gameObject, status: false);
                }
                visible = false;
            }
            else
            {
                visible = true;
            }
            visibilityCheckTimer = Random.Range(0.1f, 0.3f);
            if (explodable)
            {
                Vector3 size = GetComponent<Collider>().bounds.size;
                if (Mathf.Max(size.x, size.y, size.z) < minSizeToExplode)
                {
                    base.tag = string.Empty;
                }
            }
        }
    }
}
