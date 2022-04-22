using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class ConstructorBuildBot : MonoBehaviour
    {
        public static List<ConstructorBuildBot> buildbots = new List<ConstructorBuildBot>();

        public Transform beamOrigin;

        public GameObject constructObject;

        public BuildBotPath path;

        public int atPathIndex;

        public bool flying;

        public int _botId;

        public FMOD_CustomLoopingEmitter buildLoopingSound;

        public FMOD_CustomLoopingEmitter[] hoverLoopSounds;

        private FMOD_CustomLoopingEmitter assignedHoverLoopSound;

        private LineRenderer lineRenderer;

        public Material beamMaterial;

        private bool _launch;

        private bool _usingMenu;

        private bool _building;

        public Vector3 hoverPos;

        public bool waiting;

        public Animator animator;

        private bool hadParent;

        private bool updateRestPos;

        private Transform currentBeamPoint;

        public int botId
        {
            get
            {
                return _botId;
            }
            set
            {
                if ((bool)animator)
                {
                    SafeAnimator.SetInteger(animator, "botId", value);
                }
                _botId = value;
            }
        }

        public bool launch
        {
            get
            {
                return _launch;
            }
            set
            {
                if (_launch)
                {
                    assignedHoverLoopSound.Play();
                }
                else
                {
                    assignedHoverLoopSound.Stop();
                }
                _launch = value;
                if ((bool)animator)
                {
                    SafeAnimator.SetBool(animator, "launch", _launch);
                }
            }
        }

        public bool usingMenu
        {
            get
            {
                return _usingMenu;
            }
            set
            {
                _usingMenu = value;
                if ((bool)animator)
                {
                    SafeAnimator.SetBool(animator, "using", _usingMenu);
                }
            }
        }

        private bool building
        {
            get
            {
                return _building;
            }
            set
            {
                if (_building != value)
                {
                    currentBeamPoint = null;
                    if (value)
                    {
                        CancelInvoke("FindClosestBeamPoint");
                        InvokeRepeating("FindClosestBeamPoint", 0f, 0.3f);
                    }
                    else
                    {
                        CancelInvoke("FindClosestBeamPoint");
                    }
                }
                _building = value;
                if ((bool)animator)
                {
                    SafeAnimator.SetBool(animator, "building", _building);
                }
            }
        }

        private void FindClosestBeamPoint()
        {
            currentBeamPoint = null;
            if (constructObject == null)
            {
                building = false;
                return;
            }
            BuildBotBeamPoints componentInChildren = constructObject.GetComponentInChildren<BuildBotBeamPoints>();
            if (componentInChildren != null)
            {
                currentBeamPoint = componentInChildren.GetClosestTransform(base.transform.position);
            }
        }

        public void SetPath(BuildBotPath newpath, GameObject toConstruct)
        {
            path = newpath;
            atPathIndex = 0;
            hoverPos = path.points[0].position;
            flying = true;
            base.transform.parent = null;
            updateRestPos = false;
            GetComponent<Rigidbody>().isKinematic = false;
            building = true;
            constructObject = toConstruct;
        }

        private void Start()
        {
            buildbots.Add(this);
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            GetComponent<Rigidbody>().isKinematic = true;
            assignedHoverLoopSound = hoverLoopSounds[(buildbots.Count - 1) % hoverLoopSounds.Length];
            lineRenderer = base.gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetWidth(0.1f, 1f);
            lineRenderer.SetColors(new Color(0f, 1f, 1f, 1f), new Color(1f, 0f, 0f, 1f));
            lineRenderer.materials = new Material[1]
            {
                new Material(beamMaterial)
            };
            lineRenderer.enabled = false;
            launch = false;
            building = false;
        }

        private void OnDestroy()
        {
            buildbots.Remove(this);
        }

        public void FinishConstruction()
        {
            constructObject = null;
            path = null;
            GetComponent<Rigidbody>().isKinematic = true;
            building = false;
        }

        private void Update()
        {
            bool flag = building && currentBeamPoint != null && (currentBeamPoint.transform.position - base.transform.position).magnitude < 8f;
            lineRenderer.enabled = flag;
            if (flag)
            {
                lineRenderer.SetPosition(0, beamOrigin.position);
                lineRenderer.SetPosition(1, currentBeamPoint.transform.position);
                buildLoopingSound.Play();
            }
            else
            {
                buildLoopingSound.Stop();
            }
        }

        private void FixedUpdate()
        {
            bool flag = base.transform.parent != null;
            if (hadParent != flag && flag)
            {
                updateRestPos = true;
            }
            if (updateRestPos)
            {
                float num = Mathf.Clamp(base.transform.localPosition.magnitude, 0.05f, 3f) * 2f;
                if (num > 1f)
                {
                    base.transform.localPosition = global::UWE.Utils.SlerpVector(base.transform.localPosition, Vector3.zero, Vector3.Normalize(-base.transform.localPosition) * Time.deltaTime * num);
                }
                else
                {
                    base.transform.localPosition = global::UWE.Utils.SlerpVector(base.transform.localPosition, Vector3.zero, Time.deltaTime * num);
                }
                flying = false;
                if (base.transform.localPosition == Vector3.zero)
                {
                    updateRestPos = false;
                }
            }
            else if (flying)
            {
                Vector3 value = hoverPos - base.transform.position;
                if (value.sqrMagnitude > 1.6f)
                {
                    GetComponent<Rigidbody>().AddForce(Vector3.Normalize(value) * Time.deltaTime * 5f, ForceMode.VelocityChange);
                }
                else if ((bool)path)
                {
                    atPathIndex++;
                    if (atPathIndex >= path.points.Length)
                    {
                        atPathIndex = 0;
                    }
                    hoverPos = path.points[atPathIndex].position;
                }
            }
            hadParent = flag;
            if (waiting && !updateRestPos)
            {
                base.transform.localPosition = Vector3.up * Mathf.Sin(Time.time * 4f) * 0.2f;
            }
        }
    }
}
