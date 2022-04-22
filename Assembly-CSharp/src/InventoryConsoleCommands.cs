using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class InventoryConsoleCommands : MonoBehaviour
    {
        private void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "techtype", caseSensitiveArgs: false, combineArgs: true);
            DevConsole.RegisterConsoleCommand(this, "item");
            DevConsole.RegisterConsoleCommand(this, "madloot");
            DevConsole.RegisterConsoleCommand(this, "niceloot");
            DevConsole.RegisterConsoleCommand(this, "spawnloot");
            DevConsole.RegisterConsoleCommand(this, "tools");
            DevConsole.RegisterConsoleCommand(this, "unlockall");
            DevConsole.RegisterConsoleCommand(this, "unlock");
            DevConsole.RegisterConsoleCommand(this, "resourcesfor");
            DevConsole.RegisterConsoleCommand(this, "rotfood");
            DevConsole.RegisterConsoleCommand(this, "charge");
            DevConsole.RegisterConsoleCommand(this, "clearinventory");
            DevConsole.RegisterConsoleCommand(this, "vehicleupgrades");
            DevConsole.RegisterConsoleCommand(this, "seamothupgrades");
            DevConsole.RegisterConsoleCommand(this, "exosuitupgrades");
            DevConsole.RegisterConsoleCommand(this, "exosuitarms");
            DevConsole.RegisterConsoleCommand(this, "cyclopsupgrades");
        }

        private void OnConsoleCommand_techtype(NotificationCenter.Notification n)
        {
            if (n != null && n.data != null && n.data.Count > 0)
            {
                string text = (string)n.data[0];
                List<string> keysFor = Language.main.GetKeysFor(text, StringComparison.OrdinalIgnoreCase);
                if (keysFor.Count > 0)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < keysFor.Count; i++)
                    {
                        if (TechTypeExtensions.FromString(keysFor[i], out var techType, ignoreCase: true))
                        {
                            string text2 = techType.AsString();
                            if (stringBuilder.Length > 0)
                            {
                                stringBuilder.Append(" ");
                            }
                            stringBuilder.Append(text2);
                            ErrorMessage.AddDebug($"TechType for '{text}' string is {text2}");
                        }
                    }
                    if (stringBuilder.Length > 0)
                    {
                        GUIUtility.systemCopyBuffer = stringBuilder.ToString();
                        return;
                    }
                }
                ErrorMessage.AddDebug($"'{text}' is not a TechType");
            }
            ErrorMessage.AddDebug("Usage: techtype translated_name");
        }

        private void OnConsoleCommand_item(NotificationCenter.Notification n)
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
                int num = 1;
                if (n.data.Count > 1 && int.TryParse((string)n.data[1], out var result2))
                {
                    num = result2;
                }
                for (int i = 0; i < num; i++)
                {
                    GameObject gameObject = CraftData.InstantiateFromPrefab(result);
                    if (gameObject != null)
                    {
                        gameObject.transform.position = MainCamera.camera.transform.position + MainCamera.camera.transform.forward * 3f;
                        CrafterLogic.NotifyCraftEnd(gameObject, result);
                        Pickupable component = gameObject.GetComponent<Pickupable>();
                        if (component != null && !Inventory.main.Pickup(component))
                        {
                            ErrorMessage.AddError(Language.main.Get("InventoryFull"));
                        }
                    }
                }
            }
            else
            {
                ErrorMessage.AddDebug($"Could not find tech type for '{text}'");
            }
        }

        private void OnConsoleCommand_madloot()
        {
            CraftData.AddToInventory(TechType.Knife);
            CraftData.AddToInventory(TechType.Scanner);
            CraftData.AddToInventory(TechType.Builder);
            CraftData.AddToInventory(TechType.Titanium, 10);
            CraftData.AddToInventory(TechType.Glass, 10);
            CraftData.AddToInventory(TechType.Battery, 3);
            CraftData.AddToInventory(TechType.ComputerChip, 4);
        }

        private void OnConsoleCommand_niceloot()
        {
            CraftData.AddToInventory(TechType.RadiationSuit);
            CraftData.AddToInventory(TechType.ReinforcedDiveSuit);
            CraftData.AddToInventory(TechType.Stillsuit);
            CraftData.AddToInventory(TechType.ScrapMetal);
            CraftData.AddToInventory(TechType.StasisRifle);
            CraftData.AddToInventory(TechType.CrashPowder);
            CraftData.AddToInventory(TechType.CoralChunk);
            CraftData.AddToInventory(TechType.JeweledDiskPiece);
            CraftData.AddToInventory(TechType.Quartz);
            CraftData.AddToInventory(TechType.UraniniteCrystal);
            CraftData.AddToInventory(TechType.Nickel);
            CraftData.AddToInventory(TechType.AluminumOxide);
            CraftData.AddToInventory(TechType.Copper);
            CraftData.AddToInventory(TechType.Diamond);
            CraftData.AddToInventory(TechType.Gold);
            CraftData.AddToInventory(TechType.Kyanite);
            CraftData.AddToInventory(TechType.Lead);
            CraftData.AddToInventory(TechType.Lithium);
            CraftData.AddToInventory(TechType.Magnetite);
            CraftData.AddToInventory(TechType.Salt);
            CraftData.AddToInventory(TechType.Silver);
            CraftData.AddToInventory(TechType.StalkerTooth);
            CraftData.AddToInventory(TechType.Sulphur);
            CraftData.AddToInventory(TechType.MercuryOre);
            CraftData.AddToInventory(TechType.Tank);
            CraftData.AddToInventory(TechType.HighCapacityTank);
            CraftData.AddToInventory(TechType.Compass);
            CraftData.AddToInventory(TechType.Signal);
            CraftData.AddToInventory(TechType.RadiationHelmet);
            CraftData.AddToInventory(TechType.Rebreather);
        }

        private void OnConsoleCommand_spawnloot()
        {
            Utils.CreateNPrefabs(TechType.Copper);
            Utils.CreateNPrefabs(TechType.Gold);
            Utils.CreateNPrefabs(TechType.Magnesium);
            Utils.CreateNPrefabs(TechType.ScrapMetal);
            Utils.CreateNPrefabs(TechType.ScrapMetal);
            Utils.CreateNPrefabs(TechType.ScrapMetal);
            Utils.CreateNPrefabs(TechType.ScrapMetal);
            Utils.CreateNPrefabs(TechType.Quartz);
            Utils.CreateNPrefabs(TechType.Salt);
        }

        private void OnConsoleCommand_tools()
        {
            CraftData.AddToInventory(TechType.Scanner);
            CraftData.AddToInventory(TechType.Welder);
            CraftData.AddToInventory(TechType.Flashlight);
            CraftData.AddToInventory(TechType.Knife);
            CraftData.AddToInventory(TechType.DiveReel);
            CraftData.AddToInventory(TechType.AirBladder);
            CraftData.AddToInventory(TechType.Flare);
            CraftData.AddToInventory(TechType.Builder);
            CraftData.AddToInventory(TechType.LaserCutter);
            CraftData.AddToInventory(TechType.StasisRifle);
            CraftData.AddToInventory(TechType.PropulsionCannon);
            CraftData.AddToInventory(TechType.LEDLight);
        }

        private void GetSpawnPosition(float maxDist, out Vector3 position, out Quaternion rotation)
        {
            Transform transform = MainCamera.camera.transform;
            Vector3 forward = transform.forward;
            position = transform.position + maxDist * forward;
            Vector3 toDirection = Vector3.up;
            Vector3 origin = transform.position + forward;
            RaycastHit hitInfo = default(RaycastHit);
            if (Physics.Raycast(origin, forward, out hitInfo, maxDist))
            {
                position = hitInfo.point;
                toDirection = hitInfo.normal;
            }
            rotation = Quaternion.FromToRotation(Vector3.up, toDirection);
        }

        private void OnConsoleCommand_entityslot(NotificationCenter.Notification n)
        {
            if (n == null)
            {
                return;
            }
            int num = ((n.data != null) ? n.data.Count : 0);
            if (num > 0)
            {
                string text = (string)n.data[0];
                if (global::UWE.Utils.TryParseEnum<BiomeType>(text, out var result))
                {
                    GetSpawnPosition(10f, out var position, out var rotation);
                    GameObject obj = new GameObject("EntitySlotTest");
                    Transform component = obj.GetComponent<Transform>();
                    component.position = position;
                    component.rotation = rotation;
                    LargeWorldEntity largeWorldEntity = obj.AddComponent<LargeWorldEntity>();
                    largeWorldEntity.cellLevel = LargeWorldEntity.CellLevel.Near;
                    EntitySlot entitySlot = obj.AddComponent<EntitySlot>();
                    entitySlot.biomeType = result;
                    entitySlot.autoGenerated = true;
                    if (num > 1)
                    {
                        if (float.TryParse((string)n.data[1], out var result2))
                        {
                            entitySlot.density = result2;
                        }
                        else
                        {
                            entitySlot.density = 1f;
                        }
                    }
                    entitySlot.allowedTypes = new List<EntitySlot.Type>
                    {
                        EntitySlot.Type.Small,
                        EntitySlot.Type.Medium,
                        EntitySlot.Type.Large,
                        EntitySlot.Type.Tall,
                        EntitySlot.Type.Creature
                    };
                    LargeWorld.main.streamer.cellManager.RegisterEntity(largeWorldEntity);
                }
                else
                {
                    ErrorMessage.AddDebug($"Can't parse {text} as BiomeType");
                }
            }
            else
            {
                ErrorMessage.AddDebug("Usage: entityslot BiomeType [density]");
            }
        }

        private void OnConsoleCommand_lootprobability(NotificationCenter.Notification n)
        {
            if (n == null || n.data == null)
            {
                return;
            }
            if (global::UWE.Utils.TryParseEnum<BiomeType>((string)n.data[0], out var result))
            {
                int num = 100;
                if (n.data.Count > 1 && int.TryParse((string)n.data[1], out var result2))
                {
                    num = Mathf.Min(result2, 10000);
                }
                LargeWorldEntitySpawner spawner = LargeWorldStreamer.main.cellManager.spawner;
                GameObject gameObject = new GameObject("CSVEntitySpawner Test");
                EntitySlot entitySlot = gameObject.AddComponent<EntitySlot>();
                entitySlot.biomeType = result;
                entitySlot.allowedTypes = new List<EntitySlot.Type>
                {
                    EntitySlot.Type.Small,
                    EntitySlot.Type.Medium,
                    EntitySlot.Type.Large,
                    EntitySlot.Type.Tall,
                    EntitySlot.Type.Creature
                };
                entitySlot.density = 1f;
                entitySlot.autoGenerated = true;
                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                for (int i = 0; i < num; i++)
                {
                    string key = "None";
                    if (PrefabDatabase.TryGetPrefab(spawner.GetPrefabForSlot(entitySlot).classId, out var prefab))
                    {
                        key = prefab.name;
                    }
                    if (dictionary.TryGetValue(key, out var value))
                    {
                        dictionary[key] = value + 1;
                    }
                    else
                    {
                        dictionary.Add(key, 1);
                    }
                }
                global::UnityEngine.Object.Destroy(gameObject);
                List<string> list = new List<string>(dictionary.Keys);
                list.Sort();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("Probabilities distribution test for biome {0} ({1} test runs):\n", result, num);
                int j = 0;
                for (int count = list.Count; j < count; j++)
                {
                    string text = list[j];
                    float num2 = (float)dictionary[text] / (float)num;
                    stringBuilder.AppendFormat("{0} - {1}%\n", text, num2 * 100f);
                }
                string message = stringBuilder.ToString();
                ErrorMessage.AddDebug(message);
                Debug.Log(message);
            }
            else
            {
                ErrorMessage.AddDebug("Usage: lootprobability BiomeType [testCount]");
            }
        }

        private void OnConsoleCommand_pendingitem(NotificationCenter.Notification n)
        {
            if (n == null || n.data == null || n.data.Count <= 0)
            {
                return;
            }
            _ = (string)n.data[0];
            if (global::UWE.Utils.TryParseEnum<TechType>((string)n.data[0], out var result) && CraftData.IsAllowed(result))
            {
                GameObject gameObject = CraftData.InstantiateFromPrefab(result);
                if (gameObject != null)
                {
                    Pickupable component = gameObject.GetComponent<Pickupable>();
                    if (component != null)
                    {
                        Inventory.main.AddPending(component);
                    }
                }
            }
            else
            {
                ErrorMessage.AddDebug("Could not find tech type for tech name = " + base.name);
            }
        }

        public void OnConsoleCommand_unlockall()
        {
            KnownTech.UnlockAll(verbose: false);
        }

        private void OnConsoleCommand_unlock(NotificationCenter.Notification n)
        {
            if (n != null && n.data != null)
            {
                string text = (string)n.data[0];
                TechType result;
                if (text == "all")
                {
                    KnownTech.UnlockAll(verbose: false);
                }
                else if (global::UWE.Utils.TryParseEnum<TechType>(text, out result) && CraftData.IsAllowed(result) && KnownTech.Add(result))
                {
                    ErrorMessage.AddDebug("Unlocked " + Language.main.Get(result.AsString()));
                }
            }
        }

        private void OnConsoleCommand_unlockforced(NotificationCenter.Notification n)
        {
            if (n != null && n.data != null && global::UWE.Utils.TryParseEnum<TechType>((string)n.data[0], out var result) && CraftData.IsAllowed(result) && KnownTech.Add(result))
            {
                ErrorMessage.AddDebug("Unlocked " + Language.main.Get(result.AsString()));
            }
        }

        private void OnConsoleCommand_lock(NotificationCenter.Notification n)
        {
            if (n == null || n.data == null)
            {
                return;
            }
            string text = (string)n.data[0];
            TechType result;
            if (text == "all")
            {
                List<TechType> list = new List<TechType>(KnownTech.GetTech());
                for (int i = 0; i < list.Count; i++)
                {
                    KnownTech.Remove(list[i]);
                }
            }
            else if (global::UWE.Utils.TryParseEnum<TechType>(text, out result) && CraftData.IsAllowed(result))
            {
                _ = 0u | (KnownTech.Remove(result) ? 1u : 0u);
                PDAScanner.RemoveAllEntriesWhichUnlocks(result);
                ErrorMessage.AddDebug("Locked " + Language.main.Get(result.AsString()));
            }
        }

        private void OnConsoleCommand_addlocked(NotificationCenter.Notification n)
        {
            if (n != null && n.data != null)
            {
                int count = n.data.Count;
                if (count > 0 && global::UWE.Utils.TryParseEnum<TechType>((string)n.data[0], out var result) && CraftData.IsAllowed(result))
                {
                    int result2 = 0;
                    if (count > 1)
                    {
                        int.TryParse((string)n.data[1], out result2);
                    }
                    PDAScanner.AddByUnlockable(result, result2);
                    ErrorMessage.AddDebug($"Progress for {Language.main.Get(result.AsString())} is set to {result2}");
                    return;
                }
            }
            ErrorMessage.AddDebug("Usage: addlocked TechType [progress]");
        }

        private void OnConsoleCommand_resourcesfor(NotificationCenter.Notification n)
        {
            if (n == null || n.data == null || !global::UWE.Utils.TryParseEnum<TechType>((string)n.data[0], out var result))
            {
                return;
            }
            ITechData techData = CraftData.Get(result, skipWarnings: true);
            if (techData != null)
            {
                for (int i = 0; i < techData.ingredientCount; i++)
                {
                    IIngredient ingredient = techData.GetIngredient(i);
                    CraftData.AddToInventory(ingredient.techType, ingredient.amount, noMessage: false, spawnIfCantAdd: false);
                }
            }
        }

        private void OnConsoleCommand_fishes(NotificationCenter.Notification n)
        {
            CraftData.AddToInventory(TechType.Bleach);
            CraftData.AddToInventory(TechType.HoleFish);
            CraftData.AddToInventory(TechType.Peeper);
            CraftData.AddToInventory(TechType.Bladderfish);
            CraftData.AddToInventory(TechType.GarryFish);
            CraftData.AddToInventory(TechType.Hoverfish);
            CraftData.AddToInventory(TechType.Reginald);
            CraftData.AddToInventory(TechType.Spadefish);
            CraftData.AddToInventory(TechType.Boomerang);
            CraftData.AddToInventory(TechType.Eyeye);
            CraftData.AddToInventory(TechType.Oculus);
            CraftData.AddToInventory(TechType.Hoopfish);
            CraftData.AddToInventory(TechType.Spinefish);
            CraftData.AddToInventory(TechType.LavaBoomerang);
            CraftData.AddToInventory(TechType.LavaEyeye);
        }

        private void OnConsoleCommand_rotfood(NotificationCenter.Notification n)
        {
            foreach (InventoryItem item2 in (IEnumerable<InventoryItem>)Inventory.main.container)
            {
                Pickupable item = item2.item;
                if (item != null)
                {
                    Eatable component = item.GetComponent<Eatable>();
                    if (component != null)
                    {
                        float num = ((component.foodValue >= component.waterValue) ? component.foodValue : component.waterValue);
                        component.timeDecayStart = DayNightCycle.main.timePassedAsFloat - num / component.kDecayRate;
                    }
                }
            }
        }

        private void OnConsoleCommand_pdalog(NotificationCenter.Notification n)
        {
            if (n == null || n.data == null || n.data.Count <= 0)
            {
                return;
            }
            string text = (string)n.data[0];
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (string.Equals(text, "all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string key in PDALog.GetKeys())
                {
                    PDALog.Add(key, autoPlay: false);
                }
            }
            else
            {
                PDALog.Add(text);
            }
        }

        private void OnConsoleCommand_vehicleupgrades(NotificationCenter.Notification n)
        {
            CraftData.AddToInventory(TechType.VehicleArmorPlating);
            CraftData.AddToInventory(TechType.VehiclePowerUpgradeModule);
            CraftData.AddToInventory(TechType.VehicleStorageModule);
            CraftData.AddToInventory(TechType.LootSensorMetal);
            CraftData.AddToInventory(TechType.LootSensorLithium);
            CraftData.AddToInventory(TechType.LootSensorFragment);
        }

        private void OnConsoleCommand_seamothupgrades(NotificationCenter.Notification n)
        {
            CraftData.AddToInventory(TechType.VehicleHullModule1);
            CraftData.AddToInventory(TechType.VehicleHullModule2);
            CraftData.AddToInventory(TechType.VehicleHullModule3);
            CraftData.AddToInventory(TechType.SeamothSolarCharge);
            CraftData.AddToInventory(TechType.SeamothElectricalDefense);
            CraftData.AddToInventory(TechType.SeamothTorpedoModule);
            CraftData.AddToInventory(TechType.SeamothSonarModule);
        }

        private void OnConsoleCommand_exosuitupgrades(NotificationCenter.Notification n)
        {
            CraftData.AddToInventory(TechType.ExoHullModule1);
            CraftData.AddToInventory(TechType.ExoHullModule2);
            CraftData.AddToInventory(TechType.ExosuitThermalReactorModule);
            CraftData.AddToInventory(TechType.ExosuitJetUpgradeModule);
        }

        private void OnConsoleCommand_exosuitarms(NotificationCenter.Notification n)
        {
            CraftData.AddToInventory(TechType.ExosuitPropulsionArmModule);
            CraftData.AddToInventory(TechType.ExosuitGrapplingArmModule);
            CraftData.AddToInventory(TechType.ExosuitDrillArmModule);
            CraftData.AddToInventory(TechType.ExosuitTorpedoArmModule);
        }

        private void OnConsoleCommand_cyclopsupgrades(NotificationCenter.Notification n)
        {
            CraftData.AddToInventory(TechType.CyclopsHullModule1);
            CraftData.AddToInventory(TechType.CyclopsHullModule2);
            CraftData.AddToInventory(TechType.CyclopsHullModule3);
            CraftData.AddToInventory(TechType.PowerUpgradeModule);
            CraftData.AddToInventory(TechType.CyclopsShieldModule);
            CraftData.AddToInventory(TechType.CyclopsSonarModule);
            CraftData.AddToInventory(TechType.CyclopsSeamothRepairModule);
            CraftData.AddToInventory(TechType.CyclopsDecoyModule);
            CraftData.AddToInventory(TechType.CyclopsFireSuppressionModule);
            CraftData.AddToInventory(TechType.CyclopsThermalReactorModule);
        }

        private void OnConsoleCommand_equipment()
        {
            CraftData.AddToInventory(TechType.Fins);
            CraftData.AddToInventory(TechType.Tank);
            CraftData.AddToInventory(TechType.Compass);
            CraftData.AddToInventory(TechType.RadiationHelmet);
            CraftData.AddToInventory(TechType.RadiationGloves);
            CraftData.AddToInventory(TechType.RadiationSuit);
        }

        private void OnConsoleCommand_charge(NotificationCenter.Notification n)
        {
            if (n == null || n.data == null)
            {
                return;
            }
            float result = ((n.data.Count <= 0 || !float.TryParse((string)n.data[0], out result)) ? 1f : Mathf.Clamp01(result));
            foreach (InventoryItem item2 in (IEnumerable<InventoryItem>)Inventory.main.container)
            {
                if (item2 == null)
                {
                    continue;
                }
                Pickupable item = item2.item;
                if (item != null)
                {
                    IBattery component = item.GetComponent<IBattery>();
                    if (component != null)
                    {
                        component.charge = result * component.capacity;
                    }
                }
            }
        }

        private void OnConsoleCommand_clearinventory(NotificationCenter.Notification n)
        {
            Inventory.main.container.Clear();
            Inventory.main.equipment.ClearItems();
        }

        private void OnConsoleCommand_resizestorage(NotificationCenter.Notification n)
        {
            Inventory main = Inventory.main;
            int usedStorageCount = main.GetUsedStorageCount();
            int result;
            int result2;
            if (usedStorageCount == 0)
            {
                ErrorMessage.AddDebug("Inventory.usedStorage is empty. Nothing to resize");
            }
            else if (n != null && n.data != null && n.data.Count == 2 && int.TryParse((string)n.data[0], out result) && int.TryParse((string)n.data[1], out result2) && result >= 1 && result2 >= 1)
            {
                for (int i = 0; i < usedStorageCount; i++)
                {
                    (main.GetUsedStorage(i) as ItemsContainer)?.Resize(result, result2);
                }
            }
            else
            {
                ErrorMessage.AddDebug("Usage: 'resizestorage width height'");
            }
        }
    }
}
