using UnityEngine;

namespace AssemblyCSharp
{
    public class VehicleInterface_MapController : MonoBehaviour
    {
        [AssertNotNull]
        public GameObject interfacePrefab;

        [AssertNotNull]
        public GameObject mapHolder;

        [AssertNotNull]
        public Transform mapSpawnPos;

        [AssertNotNull]
        public GameObject playerDot;

        [AssertNotNull]
        public GameObject lightVfx;

        [AssertNotNull]
        public Seaglide seaglide;

        [AssertNotNull]
        public GameObject seaglideMesh;

        private Material seaglideIllumMat;

        private VehicleInterface_Terrain mapScript;

        private Color illumColor = Color.white;

        private GameObject mapObject;

        private void Start()
        {
            seaglideIllumMat = seaglideMesh.GetComponent<SkinnedMeshRenderer>().materials[1];
            if (mapObject == null)
            {
                mapObject = Object.Instantiate(interfacePrefab);
                mapObject.transform.SetParent(mapHolder.transform, worldPositionStays: false);
                mapObject.transform.localPosition = Vector3.zero;
                mapObject.transform.localScale = Vector3.one;
                mapObject.transform.position = mapSpawnPos.position;
                mapScript = mapObject.GetComponentInChildren<VehicleInterface_Terrain>();
            }
        }

        private void Update()
        {
            if (!seaglide.HasEnergy())
            {
                mapScript.active = false;
            }
            else if (Player.main != null && (Player.main.currentSub != null || Player.main.currentEscapePod != null || !Player.main.IsUnderwater()))
            {
                mapScript.active = false;
            }
            else if (seaglide.toggleLights.lightState == 2)
            {
                mapScript.active = false;
            }
            else
            {
                mapScript.active = true;
            }
            if (mapScript.active)
            {
                playerDot.SetActive(value: true);
                lightVfx.SetActive(value: true);
                illumColor = Color.Lerp(seaglideIllumMat.GetColor(ShaderPropertyID._GlowColor), Color.white, Time.deltaTime);
                seaglideIllumMat.SetColor(ShaderPropertyID._GlowColor, illumColor);
            }
            else
            {
                playerDot.SetActive(value: false);
                lightVfx.SetActive(value: false);
                illumColor = Color.Lerp(seaglideIllumMat.GetColor(ShaderPropertyID._GlowColor), Color.black, Time.deltaTime);
                seaglideIllumMat.SetColor(ShaderPropertyID._GlowColor, illumColor);
            }
        }
    }
}
