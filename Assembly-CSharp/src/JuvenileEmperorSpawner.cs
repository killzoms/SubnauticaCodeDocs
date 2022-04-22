using AssemblyCSharp.Story;
using UnityEngine;

namespace AssemblyCSharp
{
    public class JuvenileEmperorSpawner : MonoBehaviour
    {
        [AssertNotNull]
        public string listenForBabiesSpawnedOutsideGoal = "SeaEmperorBabiesSpawnedOutsideOfPrisonAquarium";

        [AssertNotNull]
        public GameObject juvenileEmperorPrefab;

        public float expectedLeashDistance = 500f;

        public Vector3 expectedDirectionDistanceMultiplier = Vector3.one;

        private void Start()
        {
            StoryGoalManager main = StoryGoalManager.main;
            if ((bool)main && main.IsGoalComplete(listenForBabiesSpawnedOutsideGoal))
            {
                Object.Instantiate(juvenileEmperorPrefab, base.transform.position, base.transform.rotation);
                Object.Destroy(base.gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 vector = expectedDirectionDistanceMultiplier;
            Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, new Vector3(1f / vector.x, 1f / vector.y, 1f / vector.z));
            Gizmos.color = Color.red.ToAlpha(0.5f);
            Gizmos.DrawSphere(Vector3.zero, expectedLeashDistance);
            Gizmos.DrawWireSphere(Vector3.zero, expectedLeashDistance);
        }
    }
}
