using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXSubLeakPoint : MonoBehaviour
    {
        public float waterlevel;

        public GameObject[] leakEffectPrefabs;

        private Renderer[] childRends;

        private VFXLerpColor[] childLerps;

        private GameObject leakEffect;

        private VFXWaterSpray waterSpray;

        public bool pointActive { get; private set; }

        public void Play()
        {
            childRends = base.gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
            childLerps = base.gameObject.GetComponentsInChildren<VFXLerpColor>(includeInactive: true);
            for (int i = 0; i < childLerps.Length; i++)
            {
                childLerps[i].ResetColor();
            }
            for (int j = 0; j < childRends.Length; j++)
            {
                childRends[j].enabled = true;
            }
            int num = leakEffectPrefabs.Length;
            GameObject gameObject = leakEffectPrefabs[Random.Range(0, num - 1)];
            if (gameObject != null)
            {
                leakEffect = Object.Instantiate(gameObject);
                Transform obj = leakEffect.transform;
                obj.parent = base.transform;
                Vector3 vector2 = (obj.localPosition = (obj.localEulerAngles = Vector3.zero));
            }
        }

        public void StartSpray()
        {
            if (leakEffect != null)
            {
                waterSpray = leakEffect.GetComponent<VFXWaterSpray>();
                waterSpray.Play();
            }
            pointActive = true;
        }

        public void StopSpray()
        {
            if (waterSpray != null)
            {
                waterSpray.Stop();
            }
        }

        private void DisableRenderers()
        {
            for (int i = 0; i < childRends.Length; i++)
            {
                if (childRends[i] != null)
                {
                    childRends[i].enabled = false;
                }
            }
        }

        public void Stop()
        {
            for (int i = 0; i < childLerps.Length; i++)
            {
                childLerps[i].Play();
            }
            if (waterSpray != null)
            {
                waterSpray.Stop();
            }
            Object.Destroy(leakEffect, 1.4f);
            Invoke("DisableRenderers", 1.5f);
            pointActive = false;
        }

        private void Update()
        {
            if (!(waterSpray == null))
            {
                waterSpray.waterlevel = waterlevel;
                if (base.transform.position.y > 0f && waterSpray.GetIsPlaying())
                {
                    waterSpray.Stop();
                }
            }
        }
    }
}
