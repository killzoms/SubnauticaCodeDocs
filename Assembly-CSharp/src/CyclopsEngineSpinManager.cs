using UnityEngine;

namespace AssemblyCSharp
{
    public class CyclopsEngineSpinManager : MonoBehaviour
    {
        [AssertNotNull]
        public Transform engineTransform;

        [AssertNotNull(AssertNotNullAttribute.Options.IgnorePrefabs)]
        public CyclopsMotorMode cyclopsMotorMode;

        public float[] spinSpeeds = new float[3];

        public Vector3 rotationAxis = new Vector3(0f, 1f, 0f);

        private float spinSpeed;

        private void Start()
        {
            spinSpeed = spinSpeeds[1];
        }

        private void OnEnable()
        {
            InvokeRepeating("InvokePollCyclopsMotorMode", 1f, 1f);
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        private void InvokePollCyclopsMotorMode()
        {
            spinSpeed = PollCyclopsMotorMode();
        }

        private void Update()
        {
            engineTransform.Rotate(rotationAxis * spinSpeed);
        }

        public float PollCyclopsMotorMode()
        {
            if (!cyclopsMotorMode.engineOn)
            {
                return 0f;
            }
            return cyclopsMotorMode.cyclopsMotorMode switch
            {
                CyclopsMotorMode.CyclopsMotorModes.Slow => spinSpeeds[0], 
                CyclopsMotorMode.CyclopsMotorModes.Standard => spinSpeeds[1], 
                CyclopsMotorMode.CyclopsMotorModes.Flank => spinSpeeds[2], 
                _ => 0f, 
            };
        }
    }
}
