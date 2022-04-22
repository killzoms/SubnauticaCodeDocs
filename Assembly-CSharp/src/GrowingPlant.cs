using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [SkipProtoContractCheck]
    public class GrowingPlant : HandTarget, IHandTarget
    {
        public float growthDuration = 1200f;

        [AssertNotNull]
        public AnimationCurve growthWidth;

        [AssertNotNull]
        public AnimationCurve growthHeight;

        [AssertNotNull]
        public AnimationCurve growthWidthIndoor;

        [AssertNotNull]
        public AnimationCurve growthHeightIndoor;

        public Vector3 positionOffset = Vector3.zero;

        public float heightProgressFactor;

        public Transform growingTransform;

        public GameObject grownModelPrefab;

        public Plantable seed;

        public bool isPickupable;

        private float timeStartGrowth = -1f;

        private float maxProgress = 1f;

        private bool isIndoor;

        private VFXPassYboundsToMat passYbounds;

        private VFXScaleWaving wavingScaler;

        private void OnEnable()
        {
            ShowGrowingTransform();
        }

        private void OnDisable()
        {
            growingTransform.gameObject.SetActive(value: false);
        }

        private void Update()
        {
            float progress = GetProgress();
            SetScale(growingTransform, progress);
            SetPosition(growingTransform);
            if (progress == 1f)
            {
                SpawnGrownModel();
            }
        }

        private void SpawnGrownModel()
        {
            growingTransform.gameObject.SetActive(value: false);
            GameObject gameObject = Object.Instantiate(grownModelPrefab, growingTransform.position, growingTransform.rotation);
            SetScale(gameObject.transform, 1f);
            if (isPickupable)
            {
                Plantable component = gameObject.GetComponent<Plantable>();
                if (component != null && seed.ReplaceSeedByPlant(component))
                {
                    gameObject.SetActive(value: false);
                    return;
                }
            }
            GrownPlant grownPlant = gameObject.AddComponent<GrownPlant>();
            grownPlant.seed = seed;
            grownPlant.SendMessage("OnGrown", SendMessageOptions.DontRequireReceiver);
            gameObject.transform.parent = seed.currentPlanter.grownPlantsRoot;
            seed.currentPlanter.SetupRenderers(gameObject, interior: true);
            base.enabled = false;
        }

        private void ShowGrowingTransform()
        {
            if (!growingTransform.gameObject.activeSelf)
            {
                passYbounds = growingTransform.GetComponent<VFXPassYboundsToMat>();
                if (passYbounds == null)
                {
                    wavingScaler = growingTransform.gameObject.EnsureComponent<VFXScaleWaving>();
                }
                growingTransform.gameObject.SetActive(value: true);
            }
        }

        public void SetScale(Transform tr, float progress)
        {
            float num = (isIndoor ? growthWidthIndoor.Evaluate(progress) : growthWidth.Evaluate(progress));
            float y = (isIndoor ? growthHeightIndoor.Evaluate(progress) : growthHeight.Evaluate(progress));
            tr.localScale = new Vector3(num, y, num);
            if (passYbounds != null)
            {
                passYbounds.UpdateWavingScale(tr.localScale);
            }
            else if (wavingScaler != null)
            {
                wavingScaler.UpdateWavingScale(tr.localScale);
            }
        }

        public void SetPosition(Transform tr)
        {
            Vector3 localScale = tr.localScale;
            Vector3 position = new Vector3(localScale.x * positionOffset.x, localScale.y * positionOffset.y, localScale.z * positionOffset.z);
            tr.position = base.transform.TransformPoint(position);
        }

        public void EnableIndoorState()
        {
            isIndoor = true;
        }

        private float GetGrowthDuration()
        {
            float num = (NoCostConsoleCommand.main.fastGrowCheat ? 0.01f : 1f);
            return growthDuration * num;
        }

        public float GetProgress()
        {
            if (timeStartGrowth == -1f)
            {
                SetProgress(0f);
                return 0f;
            }
            return Mathf.Clamp((float)(DayNightCycle.main.timePassed - (double)timeStartGrowth) / GetGrowthDuration(), 0f, maxProgress);
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp(progress, 0f, maxProgress);
            SetScale(growingTransform, progress);
            timeStartGrowth = DayNightCycle.main.timePassedAsFloat - GetGrowthDuration() * progress;
        }

        public void SetMaxHeight(float height)
        {
            if (!(heightProgressFactor <= 0f))
            {
                if (GetProgress() >= maxProgress)
                {
                    SetProgress(maxProgress);
                }
                maxProgress = Mathf.Clamp01(height * heightProgressFactor);
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            if (base.enabled)
            {
                string format = Language.main.GetFormat("GrowingPlant", Language.main.Get(seed.plantTechType.AsString()));
                HandReticle.main.SetInteractText(format, translate: false);
                HandReticle.main.SetProgress(GetProgress());
                HandReticle.main.SetIcon(HandReticle.IconType.Progress, 1.5f);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
        }
    }
}
