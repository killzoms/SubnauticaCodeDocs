using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PlaceTool : PlayerTool
    {
        private enum SurfaceType
        {
            Floor,
            Wall,
            Ceiling
        }

        public FMODAsset placementSound;

        public bool allowedOnWalls;

        public bool allowedOnGround = true;

        public bool allowedOnCeiling;

        public bool allowedInBase = true;

        public bool allowedOnBase = true;

        public bool allowedOutside;

        public bool allowedOnConstructable = true;

        public GameObject ghostModelPrefab;

        public bool alignWithSurface;

        public bool hideInvalidGhostModel;

        public bool rotationEnabled;

        public bool allowedOnRigidBody;

        private GameObject ghostModel;

        private Renderer[] renderers;

        private bool validPosition;

        private float additiveRotation;

        private static Material ghostStructureMaterial;

        private static readonly Color placeColorAllow = new Color(0f, 1f, 0f, 1f);

        private static readonly Color placeColorDeny = new Color(1f, 0f, 0f, 1f);

        private static List<OrientedBounds> localBounds = new List<OrientedBounds>();

        private bool usedThisFrame;

        protected static LayerMask placeLayerMask => ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Trigger")));

        private bool Place()
        {
            bool flag = false;
            if (validPosition)
            {
                flag = true;
                Vector3 position = ghostModel.transform.position;
                Quaternion rotation = ghostModel.transform.rotation;
                GetComponent<Pickupable>().Drop();
                base.transform.position = position;
                base.transform.rotation = rotation;
                SubRoot currentSub = Player.main.GetCurrentSub();
                if (currentSub != null)
                {
                    base.transform.parent = currentSub.GetModulesRoot();
                    LargeWorldEntity component = GetComponent<LargeWorldEntity>();
                    if ((bool)component)
                    {
                        component.enabled = false;
                    }
                }
                SkyEnvironmentChanged.Send(base.gameObject, currentSub);
                Rigidbody component2 = GetComponent<Rigidbody>();
                if ((bool)component2)
                {
                    component2.isKinematic = true;
                }
                usedThisFrame = false;
            }
            if (flag)
            {
                if (placementSound != null)
                {
                    Utils.PlayFMODAsset(placementSound, base.transform);
                }
                OnPlace();
            }
            return flag;
        }

        public override void OnToolUseAnim(GUIHand guiHand)
        {
            Place();
        }

        public override bool OnRightHandDown()
        {
            if (!hasAnimations)
            {
                return Place();
            }
            if (validPosition)
            {
                usedThisFrame = true;
            }
            return false;
        }

        public override bool OnRightHandUp()
        {
            if (hasAnimations)
            {
                usedThisFrame = false;
            }
            return false;
        }

        public override bool GetUsedToolThisFrame()
        {
            return usedThisFrame;
        }

        private void LateUpdate()
        {
            if (!(usingPlayer != null))
            {
                return;
            }
            Transform aimTransform = Builder.GetAimTransform();
            RaycastHit hitInfo;
            bool flag = Physics.Raycast(aimTransform.position, aimTransform.forward, out hitInfo, 5f, placeLayerMask, QueryTriggerInteraction.Ignore);
            Vector3 forward = Vector3.forward;
            Vector3 up = Vector3.up;
            Vector3 position;
            if (flag)
            {
                SurfaceType surfaceType = SurfaceType.Floor;
                if (Mathf.Abs(hitInfo.normal.y) < 0.3f)
                {
                    surfaceType = SurfaceType.Wall;
                }
                else if (hitInfo.normal.y < 0f)
                {
                    surfaceType = SurfaceType.Ceiling;
                }
                position = hitInfo.point;
                if (alignWithSurface || surfaceType == SurfaceType.Wall)
                {
                    forward = hitInfo.normal;
                    up = Vector3.up;
                }
                else
                {
                    forward = new Vector3(0f - aimTransform.forward.x, 0f, 0f - aimTransform.forward.z).normalized;
                    up = Vector3.up;
                }
                switch (surfaceType)
                {
                    case SurfaceType.Floor:
                        validPosition = allowedOnGround;
                        break;
                    case SurfaceType.Ceiling:
                        validPosition = allowedOnCeiling;
                        break;
                    case SurfaceType.Wall:
                        validPosition = allowedOnWalls;
                        break;
                }
            }
            else
            {
                position = aimTransform.position + aimTransform.forward * 1.5f;
                forward = -aimTransform.forward;
                up = Vector3.up;
                validPosition = false;
            }
            if (GameInput.GetButtonHeld(Builder.buttonRotateCW))
            {
                additiveRotation = MathExtensions.RepeatAngle(additiveRotation - Time.deltaTime * Builder.additiveRotationSpeed);
            }
            else if (GameInput.GetButtonHeld(Builder.buttonRotateCCW))
            {
                additiveRotation = MathExtensions.RepeatAngle(additiveRotation + Time.deltaTime * Builder.additiveRotationSpeed);
            }
            Quaternion rotation = Quaternion.LookRotation(forward, up);
            if (rotationEnabled)
            {
                rotation *= Quaternion.AngleAxis(additiveRotation, up);
            }
            ghostModel.transform.position = position;
            ghostModel.transform.rotation = rotation;
            if (flag)
            {
                Rigidbody componentInParent = hitInfo.collider.gameObject.GetComponentInParent<Rigidbody>();
                validPosition = validPosition && (componentInParent == null || componentInParent.isKinematic || allowedOnRigidBody);
            }
            SubRoot currentSub = Player.main.GetCurrentSub();
            bool flag2 = false;
            if (flag)
            {
                flag2 = hitInfo.collider.gameObject.GetComponentInParent<SubRoot>() != null;
            }
            if (flag && hitInfo.collider.gameObject.CompareTag("DenyBuilding"))
            {
                validPosition = false;
            }
            if (currentSub == null)
            {
                validPosition = validPosition && (allowedOnBase || !flag2);
            }
            if (((allowedInBase && (bool)currentSub) || (allowedOutside && !currentSub)) && flag)
            {
                GameObject gameObject = global::UWE.Utils.GetEntityRoot(hitInfo.collider.gameObject);
                if (!gameObject)
                {
                    SceneObjectIdentifier componentInParent2 = hitInfo.collider.GetComponentInParent<SceneObjectIdentifier>();
                    gameObject = ((!componentInParent2) ? hitInfo.collider.gameObject : componentInParent2.gameObject);
                }
                if (currentSub == null)
                {
                    validPosition = validPosition && Builder.ValidateOutdoor(gameObject);
                }
                if (!allowedOnConstructable)
                {
                    validPosition = validPosition && gameObject.GetComponentInParent<Constructable>() == null;
                }
                validPosition &= Builder.CheckSpace(position, rotation, localBounds, placeLayerMask, hitInfo.collider);
            }
            else
            {
                validPosition = false;
            }
            MaterialExtensions.SetColor(renderers, ShaderPropertyID._Tint, validPosition ? placeColorAllow : placeColorDeny);
            if (hideInvalidGhostModel)
            {
                ghostModel.SetActive(validPosition);
            }
        }

        protected override void OnDestroy()
        {
            DestroyGhostModel();
            base.OnDestroy();
        }

        private void CreateGhostModel()
        {
            if (!(ghostModel == null))
            {
                return;
            }
            GameObject original = ((ghostModelPrefab != null) ? ghostModelPrefab : base.gameObject);
            ghostModel = Object.Instantiate(original);
            if (ghostStructureMaterial == null)
            {
                ghostStructureMaterial = new Material(Resources.Load<Material>("Materials/ghostmodel"));
            }
            renderers = ghostModel.GetComponentsInChildren<Renderer>();
            MaterialExtensions.AssignMaterial(ghostModel, ghostStructureMaterial);
            MonoBehaviour[] componentsInChildren = ghostModel.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            foreach (MonoBehaviour monoBehaviour in componentsInChildren)
            {
                if ((bool)monoBehaviour)
                {
                    monoBehaviour.enabled = false;
                }
            }
            Collider[] componentsInChildren2 = ghostModel.GetComponentsInChildren<Collider>(includeInactive: true);
            for (int i = 0; i < componentsInChildren2.Length; i++)
            {
                componentsInChildren2[i].enabled = false;
            }
        }

        private void DestroyGhostModel()
        {
            if (ghostModel != null)
            {
                Object.Destroy(ghostModel);
                ghostModel = null;
            }
        }

        public override void OnDraw(Player p)
        {
            base.OnDraw(p);
            CreateGhostModel();
            if (ghostModel != null)
            {
                Builder.CacheBounds(base.transform, base.gameObject, localBounds);
            }
            if (rotationEnabled)
            {
                Builder.ShowRotationControlsHint();
            }
        }

        public override void OnHolster()
        {
            base.OnHolster();
            DestroyGhostModel();
            localBounds.Clear();
        }

        public virtual void OnPlace()
        {
            localBounds.Clear();
        }
    }
}
