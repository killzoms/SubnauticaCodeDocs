using UnityEngine;

namespace AssemblyCSharp
{
    public class SpawnConsoleCommand : MonoBehaviour
    {
        private void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "spawn");
        }

        private void OnConsoleCommand_spawn(NotificationCenter.Notification n)
        {
            if (n == null || n.data == null || n.data.Count <= 0)
            {
                return;
            }
            string text = (string)n.data[0];
            if (global::UWE.Utils.TryParseEnum<TechType>(text, out var result))
            {
                if (!CraftData.IsAllowed(result))
                {
                    return;
                }
                GameObject prefabForTechType = CraftData.GetPrefabForTechType(result);
                if (prefabForTechType != null)
                {
                    int num = 1;
                    if (n.data.Count > 1 && int.TryParse((string)n.data[1], out var result2))
                    {
                        num = result2;
                    }
                    float maxDist = 12f;
                    if (n.data.Count > 2)
                    {
                        maxDist = float.Parse((string)n.data[2]);
                    }
                    Debug.LogFormat("Spawning {0} {1}", num, result);
                    for (int i = 0; i < num; i++)
                    {
                        GameObject obj = Utils.CreatePrefab(prefabForTechType, maxDist, i > 0);
                        LargeWorldEntity.Register(obj);
                        CrafterLogic.NotifyCraftEnd(obj, result);
                        obj.SendMessage("StartConstruction", SendMessageOptions.DontRequireReceiver);
                    }
                }
                else
                {
                    ErrorMessage.AddDebug("Could not find prefab for TechType = " + result);
                }
            }
            else
            {
                ErrorMessage.AddDebug("Could not parse " + text + " as TechType");
            }
        }
    }
}
