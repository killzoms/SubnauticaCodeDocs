using System;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public class Cleaner : MonoBehaviour
    {
        private void Start()
        {
            InvokeRepeating("DoUnloadUnusedAssets", global::UnityEngine.Random.value, 1f);
            InvokeRepeating("DoCollectGarbage", global::UnityEngine.Random.value, 1f);
        }

        private void DoUnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidCallingProblematicMethodsRule")]
        private void DoCollectGarbage()
        {
            GC.Collect();
        }
    }
}
