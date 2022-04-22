using UnityEngine;

namespace AssemblyCSharp
{
    public class BaseFiltrationMachineGeometry : MonoBehaviour, IBaseModuleGeometry, IObstacle
    {
        public Animator animator;

        public GameObject sparksPrefab;

        public GameObject fabLight;

        public GameObject leftBeam;

        public GameObject rightBeam;

        private GameObject sparksL;

        private GameObject sparksR;

        private bool scanning;

        private float currentYPos;

        private Base.Face _geometryFace;

        public Base.Face geometryFace
        {
            get
            {
                return _geometryFace;
            }
            set
            {
                _geometryFace = value;
            }
        }

        private void Start()
        {
            Vector3 position = base.transform.position;
            position.y = currentYPos;
            sparksL = Utils.SpawnPrefabAt(sparksPrefab, leftBeam.transform, leftBeam.transform.position);
            sparksR = Utils.SpawnPrefabAt(sparksPrefab, rightBeam.transform, rightBeam.transform.position);
        }

        public void SetWorking(bool working, float yPos)
        {
            if ((bool)animator)
            {
                animator.SetBool(AnimatorHashID.fabricating, working);
            }
            if (working && !scanning)
            {
                StartFabricating();
            }
            else if (!working && scanning)
            {
                StopFabricating();
            }
            if (scanning)
            {
                Vector3 position = base.transform.position;
                position.y = yPos;
                Shader.SetGlobalFloat(ShaderPropertyID._FabricatorPosY, yPos + 0.03f);
                if (sparksL != null && sparksR != null)
                {
                    sparksR.transform.position = GetBeamEnd(rightBeam.transform.position, rightBeam.transform.forward, position, Vector3.up);
                    sparksL.transform.position = GetBeamEnd(leftBeam.transform.position, leftBeam.transform.forward, position, Vector3.up);
                }
            }
        }

        public void StartFabricating()
        {
            leftBeam.SetActive(value: true);
            rightBeam.SetActive(value: true);
            fabLight.SetActive(value: true);
            if ((bool)sparksL)
            {
                sparksL.GetComponent<ParticleSystem>().Play();
            }
            if ((bool)sparksR)
            {
                sparksR.GetComponent<ParticleSystem>().Play();
            }
            scanning = true;
        }

        public void StopFabricating()
        {
            leftBeam.SetActive(value: false);
            rightBeam.SetActive(value: false);
            fabLight.SetActive(value: false);
            sparksL.GetComponent<ParticleSystem>().Stop();
            sparksR.GetComponent<ParticleSystem>().Stop();
            scanning = false;
        }

        public void OnHover(HandTargetEventData eventData)
        {
            FiltrationMachine module = GetModule();
            if (module != null)
            {
                module.OnHover(eventData);
            }
        }

        public void OnUse(HandTargetEventData eventData)
        {
            FiltrationMachine module = GetModule();
            if (module != null)
            {
                module.OnUse(this);
            }
        }

        private FiltrationMachine GetModule()
        {
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null)
            {
                IBaseModule module = componentInParent.GetModule(geometryFace);
                if (module != null)
                {
                    return module as FiltrationMachine;
                }
            }
            return null;
        }

        private static Vector3 GetBeamEnd(Vector3 beamPos, Vector3 beamRot, Vector3 basePos, Vector3 baseRot)
        {
            return beamPos + Vector3.Normalize(beamRot) * (Vector3.Dot(basePos - beamPos, baseRot) / Vector3.Dot(beamRot, baseRot));
        }

        public bool CanDeconstruct(out string reason)
        {
            FiltrationMachine module = GetModule();
            if (module != null && module.storageContainer.container.count > 0)
            {
                reason = Language.main.Get("DeconstructNonEmptyFiltrationMachineError");
                return false;
            }
            reason = null;
            return true;
        }
    }
}
