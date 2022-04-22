using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class JointHelper : MonoBehaviour
    {
        [ProtoMember(1)]
        public string connectedObjectUid;

        [ProtoMember(2)]
        public string jointType;

        public bool isPlaceholder;

        public void Start()
        {
            if (string.IsNullOrEmpty(connectedObjectUid) || string.IsNullOrEmpty(jointType))
            {
                if (!isPlaceholder)
                {
                    Debug.LogWarning("No joint target on " + base.name, base.gameObject);
                }
                return;
            }
            if (!UniqueIdentifier.TryGetIdentifier(connectedObjectUid, out var uid))
            {
                Debug.LogWarning("Joint target not found on " + base.name, base.gameObject);
                return;
            }
            Rigidbody component = uid.GetComponent<Rigidbody>();
            if (!component)
            {
                Debug.LogWarning("No Rigidbody on joint target of " + base.name, base.gameObject);
                return;
            }
            Type type = GetJointType(jointType);
            if (type == null)
            {
                Debug.LogWarning("Missing joint type " + jointType, base.gameObject);
            }
            else
            {
                Connect(component, type);
            }
        }

        private bool Connect(Rigidbody target, Type jointType)
        {
            Joint joint = (Joint)base.gameObject.EnsureComponent(jointType);
            joint.connectedBody = target;
            base.gameObject.SendMessage("OnJointConnected", joint, SendMessageOptions.DontRequireReceiver);
            return true;
        }

        public void OnExamine()
        {
            global::UnityEngine.Object.Destroy(base.gameObject.GetComponent<Joint>());
            connectedObjectUid = null;
            jointType = null;
        }

        public static bool ConnectFixed(GameObject source, Rigidbody target)
        {
            UniqueIdentifier component = target.GetComponent<UniqueIdentifier>();
            if (component == null)
            {
                return false;
            }
            JointHelper jointHelper = source.AddComponent<JointHelper>();
            jointHelper.connectedObjectUid = component.Id;
            jointHelper.jointType = "FixedJoint";
            return true;
        }

        public static bool ConnectFixed(JointHelper helper, Rigidbody target)
        {
            UniqueIdentifier component = target.GetComponent<UniqueIdentifier>();
            if (component == null)
            {
                helper.connectedObjectUid = null;
                helper.jointType = null;
                return false;
            }
            helper.connectedObjectUid = component.Id;
            helper.jointType = "FixedJoint";
            return helper.Connect(target, typeof(FixedJoint));
        }

        public static void Disconnect(Joint joint, bool destroyHelper)
        {
            if (destroyHelper)
            {
                global::UnityEngine.Object.Destroy(joint.GetComponent<JointHelper>());
            }
            global::UnityEngine.Object.Destroy(joint);
        }

        private static Type GetJointType(string jointType)
        {
            return jointType switch
            {
                "Joint" => typeof(Joint), 
                "CharacterJoint" => typeof(CharacterJoint), 
                "HingeJoint" => typeof(HingeJoint), 
                "SpringJoint" => typeof(SpringJoint), 
                "FixedJoint" => typeof(FixedJoint), 
                "ConfigurableJoint" => typeof(ConfigurableJoint), 
                _ => null, 
            };
        }
    }
}
