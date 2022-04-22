namespace AssemblyCSharp
{
    public class MagmaPillarSpawn : PrefabSpawn
    {
        public bool initializedRandomTime;

        public override void SpawnObj()
        {
            base.SpawnObj();
            if (!initializedRandomTime)
            {
                spawnedObj.GetComponent<MagmaPillar>().SetRandomTimeLived();
                initializedRandomTime = true;
            }
        }

        public override bool GetTimeToSpawn()
        {
            if (!initializedRandomTime)
            {
                return true;
            }
            return base.GetTimeToSpawn();
        }
    }
}
