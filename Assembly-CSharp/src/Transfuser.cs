using UnityEngine;

namespace AssemblyCSharp
{
    public class Transfuser : PlayerTool
    {
        public FMOD_StudioEventEmitter takeSampleSound;

        public FMOD_StudioEventEmitter injectSerumSound;

        public FMOD_StudioEventEmitter invalidUseSound;

        public Serum heldSerum;

        private float timeLastSerumAction;

        public string heldSampleName;

        public GameObject liquidMesh;

        private float liquidLevel;

        public float energyCost = 5f;

        private void CreateHeldSerum(Creature cb)
        {
            string activeBehaviourName = cb.GetActiveBehaviourName();
            DNADatabaseRow rowForBehavior = DNADatabase.main.GetRowForBehavior(activeBehaviourName);
            if (rowForBehavior != null)
            {
                GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gameObject.name = "HeldSerum";
                gameObject.GetComponent<Renderer>().material.color = Utils.HexStringToColor(rowForBehavior.color);
                gameObject.transform.parent = MainCameraControl.main.cameraOffsetTransform;
                gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                gameObject.transform.localPosition = new Vector3(0f, -0.25f, 1.5f);
                heldSerum = gameObject.AddComponent<Serum>();
                heldSerum.dnaEntry = rowForBehavior;
                iTween.ScaleFrom(gameObject, iTween.Hash("scale", new Vector3(0.01f, 0.01f, 0.01f), "time", 0.4, "easetype", iTween.EaseType.easeInOutCubic));
                liquidLevel = 0f;
                Material sharedMaterial = (Material)Resources.Load(rowForBehavior.transfuserMaterial);
                liquidMesh.GetComponent<Renderer>().sharedMaterial = sharedMaterial;
                liquidMesh.GetComponent<Renderer>().enabled = true;
                heldSampleName = "LMG to inject " + rowForBehavior.description + " (e for self)";
                if ((bool)takeSampleSound)
                {
                    Utils.PlayEnvSound(takeSampleSound);
                }
            }
            else
            {
                Debug.Log("Transfuser.CreateHeldSerum() - Couldn't find database row to make serum for creature behavior \"" + activeBehaviourName + "\"");
            }
        }

        public string GetHUDText()
        {
            string result = "";
            if (Player.main.IsUnderwater() && !Player.main.IsInSub())
            {
                result = "RMB to sample lifeform DNA";
                if (heldSampleName != "")
                {
                    result = heldSampleName;
                }
            }
            return result;
        }

        public override void OnHolster()
        {
            base.OnHolster();
            if ((bool)heldSerum)
            {
                heldSerum.gameObject.SetActive(value: false);
            }
        }

        public override void OnDraw(Player p)
        {
            base.OnDraw(p);
            if ((bool)heldSerum)
            {
                heldSerum.gameObject.SetActive(value: true);
            }
        }

        private void UpdateSerumTool()
        {
            if (!Player.main.GetRightHandDown() || !(Time.time > timeLastSerumAction + 0.5f) || !(energyMixin.charge >= energyCost))
            {
                return;
            }
            int num = global::UWE.Utils.RaycastIntoSharedBuffer(MainCameraControl.main.transform.position, MainCameraControl.main.transform.forward, 3f);
            for (int i = 0; i < num; i++)
            {
                RaycastHit raycastHit = global::UWE.Utils.sharedHitBuffer[i];
                Creature component = raycastHit.collider.gameObject.GetComponent<Creature>();
                if (!(heldSerum != null) && heldSerum == null && (bool)component)
                {
                    CreateHeldSerum(component);
                    timeLastSerumAction = Time.time;
                    energyMixin.ConsumeEnergy(energyCost);
                    return;
                }
            }
            Utils.PlayEnvSound(invalidUseSound);
        }

        private void UpdateLiquidLevel()
        {
            liquidLevel += Time.deltaTime * 0.5f;
            Shader.SetGlobalFloat(ShaderPropertyID._TransfuserLevel, Mathf.Clamp01(liquidLevel));
        }

        private void Update()
        {
            if ((bool)usingPlayer)
            {
                UpdateSerumTool();
                UpdateLiquidLevel();
            }
        }
    }
}
