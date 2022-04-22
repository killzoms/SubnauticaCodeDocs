using AssemblyCSharp.Story;
using UnityEngine;

namespace AssemblyCSharp
{
    public class SeaEmperorBabiesSpawner : MonoBehaviour, ICompileTimeCheckable
    {
        [AssertNotNull]
        public string listenForBabiesLeftPrisonGoal = "SeaEmperorBabiesLeftPrisonAquarium";

        [AssertNotNull]
        public StoryGoal babiesSpawnedGoal;

        [AssertNotNull]
        public GameObject babyEmperorPrefab;

        [AssertNotNull]
        public Transform[] spawnPoints;

        public float babyScale = 2.02f;

        private void Start()
        {
            StoryGoalManager main = StoryGoalManager.main;
            if (!main || !main.IsGoalComplete(listenForBabiesLeftPrisonGoal))
            {
                return;
            }
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                GameObject gameObject = Object.Instantiate(babyEmperorPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
                if (gameObject != null)
                {
                    SeaEmperorBaby component = gameObject.GetComponent<SeaEmperorBaby>();
                    if ((bool)component)
                    {
                        component.SetScale(babyScale);
                        component.dropEnzymes.enabled = true;
                        component.dropEnzymes.spawnLimit = 5;
                        Vector3 position = component.transform.position - component.transform.forward * 3f;
                        GameObject go = Object.Instantiate(component.dropEnzymes.enzymePrefab, position, Quaternion.identity);
                        LargeWorldEntity.Register(gameObject);
                        LargeWorldEntity.Register(go);
                    }
                }
            }
            babiesSpawnedGoal.Trigger();
            Object.Destroy(base.gameObject);
        }

        public string CompileTimeCheck()
        {
            return StoryGoalUtils.CheckStoryGoal(babiesSpawnedGoal);
        }
    }
}
