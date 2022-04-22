using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [RequireComponent(typeof(Sealed))]
    public class LaserCutObject : MonoBehaviour
    {
        public GameObject nodeHolderFront;

        public GameObject nodeHolderBack;

        public GameObject laserCutStreak;

        public GameObject laserCutFX;

        public Sealed sealedScript;

        public GameObject uncutObject;

        public GameObject cutObject;

        public GameObject cutFloatAwayObject;

        private Material cutObjectMat;

        private Material cutFloatAwayMat;

        private bool cutting;

        private int totalNodes;

        private float lastCutValue;

        private float smoothingScalar;

        private Vector3 matchingNodePos;

        private float doorTimer;

        [NonSerialized]
        [ProtoMember(1)]
        public bool isCutOpen;

        private void OnEnable()
        {
            totalNodes = nodeHolderFront.transform.childCount;
            lastCutValue = sealedScript.openedAmount;
            smoothingScalar = 0f;
            matchingNodePos = Vector3.zero;
            if (isCutOpen)
            {
                CutOpenDoor();
            }
            cutObjectMat = cutObject.GetComponent<MeshRenderer>().material;
            if ((bool)cutFloatAwayObject)
            {
                cutFloatAwayMat = cutFloatAwayObject.GetComponent<MeshRenderer>().material;
            }
        }

        private void Update()
        {
            if (isCutOpen)
            {
                if (cutObjectMat != null)
                {
                    float @float = cutObjectMat.GetFloat("_GlowStrength");
                    if (@float > 0f)
                    {
                        @float = Mathf.MoveTowards(@float, 0f, Time.deltaTime / 9f);
                        cutObjectMat.SetFloat(ShaderPropertyID._GlowStrength, @float);
                        cutObjectMat.SetFloat(ShaderPropertyID._GlowStrengthNight, @float);
                        if ((bool)cutFloatAwayObject)
                        {
                            cutFloatAwayMat.SetFloat(ShaderPropertyID._GlowStrength, @float);
                            cutFloatAwayMat.SetFloat(ShaderPropertyID._GlowStrengthNight, @float);
                        }
                    }
                }
                if ((bool)cutFloatAwayObject && Time.time - doorTimer > 300f)
                {
                    float x = cutFloatAwayObject.transform.localScale.x;
                    if (x > 0.05f)
                    {
                        x = Mathf.Lerp(x, 0f, Time.deltaTime * 5f);
                        cutFloatAwayObject.transform.localScale = new Vector3(x, x, x);
                    }
                    else
                    {
                        cutFloatAwayObject.SetActive(value: false);
                    }
                }
            }
            float openedAmount = sealedScript.openedAmount;
            if (cutting && !isCutOpen)
            {
                for (int i = 0; i < laserCutFX.transform.childCount; i++)
                {
                    ParticleSystem component = laserCutFX.transform.GetChild(i).GetComponent<ParticleSystem>();
                    if ((bool)component)
                    {
                        ParticleSystem.EmissionModule emission = component.emission;
                        emission.enabled = true;
                        if (!component.isPlaying)
                        {
                            component.Play();
                        }
                    }
                }
                float maxOpenedAmount = sealedScript.maxOpenedAmount;
                float num = openedAmount / maxOpenedAmount * (float)totalNodes;
                int num2 = (int)Mathf.Floor(num);
                if (num2 > 0)
                {
                    _ = num % (float)num2;
                }
                int num3 = num2 + 1;
                if (num3 >= totalNodes)
                {
                    num3 = 0;
                }
                if (num2 >= totalNodes)
                {
                    num2 = 0;
                }
                bool num4 = Utils.CheckObjectInFront(base.transform, Player.main.transform);
                float y = (num4 ? 0f : 180f);
                laserCutFX.transform.localRotation = Quaternion.Euler(new Vector3(0f, y, 0f));
                laserCutFX.transform.Rotate(new Vector3(0f, -90f, 0f));
                Transform obj = (num4 ? nodeHolderFront.transform : nodeHolderBack.transform);
                Transform transform = obj.GetChild(num3).transform;
                Transform transform2 = obj.GetChild(num2).transform;
                if (transform != null && transform2 != null)
                {
                    Vector3 position = transform.position;
                    if (matchingNodePos == Vector3.zero)
                    {
                        matchingNodePos = position;
                    }
                    else
                    {
                        matchingNodePos = Vector3.Lerp(matchingNodePos, position, Time.deltaTime * 2f);
                    }
                    laserCutStreak.transform.position = matchingNodePos;
                    laserCutFX.transform.position = matchingNodePos;
                    if ((bool)Player.main.gameObject)
                    {
                        Player.main.armsController.lookTargetTransform.position = matchingNodePos;
                    }
                }
                lastCutValue = openedAmount;
            }
            else
            {
                for (int j = 0; j < laserCutFX.transform.childCount; j++)
                {
                    ParticleSystem component2 = laserCutFX.transform.GetChild(j).GetComponent<ParticleSystem>();
                    if ((bool)component2)
                    {
                        ParticleSystem.EmissionModule emission2 = component2.emission;
                        emission2.enabled = false;
                    }
                }
            }
            if (!sealedScript.IsSealed() && !isCutOpen)
            {
                CutOpenDoor();
            }
        }

        public void CutOpenDoor()
        {
            isCutOpen = true;
            uncutObject.SetActive(value: false);
            cutObject.SetActive(value: true);
            doorTimer = Time.time;
        }

        public void ActivateFX()
        {
            cutting = true;
            CancelInvoke("StopCutting");
            Invoke("StopCutting", 1f);
        }

        private void StopCutting()
        {
            cutting = false;
        }
    }
}
