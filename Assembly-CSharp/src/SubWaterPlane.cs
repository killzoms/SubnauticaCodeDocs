using UnityEngine;

namespace AssemblyCSharp
{
    public class SubWaterPlane : MonoBehaviour
    {
        private static float visThresh = 0.0001f;

        public Shader depthClipShader;

        public Renderer waterRender;

        public float _leakAmount;

        public bool isFlooding;

        private Transform[] children;

        private float minYPos;

        private float maxYPos;

        private Vector3 extentsPos;

        private Vector3 extentsSize;

        private Transform hostTrans;

        private SubRoot subRoot;

        private Renderer clipRender;

        public float leakAmount
        {
            get
            {
                return _leakAmount;
            }
            set
            {
                _leakAmount = value;
                bool active = value > visThresh;
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].gameObject != base.gameObject)
                    {
                        children[i].gameObject.SetActive(active);
                    }
                }
            }
        }

        public void SetHost(Transform host)
        {
            hostTrans = host;
            subRoot = host.gameObject.GetComponent<SubRoot>();
            if (subRoot != null && subRoot.depthClearer != null)
            {
                GameObject gameObject = Object.Instantiate(subRoot.depthClearer.gameObject, subRoot.depthClearer.transform.position, subRoot.depthClearer.transform.rotation);
                gameObject.name = "Depth clip for water plane";
                gameObject.transform.parent = subRoot.depthClearer.transform.parent;
                clipRender = gameObject.GetComponent<Renderer>();
                clipRender.castShadows = false;
                clipRender.receiveShadows = false;
                clipRender.material.renderQueue = 3500;
                clipRender.material.shader = depthClipShader;
                waterRender.material.renderQueue = 3501;
                clipRender.enabled = false;
            }
        }

        public void OnPlayerEntered(Player p)
        {
            waterRender.enabled = true;
            if (clipRender != null)
            {
                clipRender.enabled = true;
            }
        }

        public void OnPlayerExited(Player p)
        {
            waterRender.enabled = false;
            if (clipRender != null)
            {
                clipRender.enabled = false;
            }
        }

        private void Awake()
        {
            BoxCollider component = GetComponent<BoxCollider>();
            extentsPos = component.center;
            extentsSize = component.size;
            Object.Destroy(component);
            children = base.gameObject.GetComponentsInChildren<Transform>();
            leakAmount = 0f;
        }

        private void Update()
        {
            if (leakAmount > visThresh)
            {
                isFlooding = true;
                Vector3 position = hostTrans.position;
                Vector3 eulerAngles = new Vector3(0f, hostTrans.eulerAngles.y, 0f);
                _ = base.transform.forward;
                float num = Mathf.Abs(hostTrans.TransformDirection(extentsSize).y) * 0.5f;
                minYPos = 0f - num + extentsPos.y;
                maxYPos = num + extentsPos.y;
                position.y = position.y + minYPos + (maxYPos - minYPos) * leakAmount;
                base.transform.position = position;
                base.transform.eulerAngles = eulerAngles;
            }
            else if (isFlooding)
            {
                base.transform.position = new Vector3(0f, -10000f, 0f);
                isFlooding = false;
            }
        }
    }
}
