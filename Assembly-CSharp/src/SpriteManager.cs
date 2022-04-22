using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class SpriteManager
    {
        public enum Group
        {
            None,
            Item,
            Background,
            Category,
            Log,
            Tab,
            Pings,
            ItemActions
        }

        private const string path = "Sprites/";

        private static readonly Dictionary<Group, string> mapping;

        private static Atlas.Sprite _defaultSprite;

        private static Dictionary<Group, Dictionary<string, Atlas.Sprite>> groups;

        public static Atlas.Sprite defaultSprite
        {
            get
            {
                if (_defaultSprite == null)
                {
                    _defaultSprite = GetWithNoDefault(Group.None, "Unknown");
                }
                return _defaultSprite;
            }
        }

        static SpriteManager()
        {
            mapping = new Dictionary<Group, string>
            {
                {
                    Group.Item,
                    "Items"
                },
                {
                    Group.Category,
                    "Categories"
                },
                {
                    Group.Tab,
                    "Tabs"
                },
                {
                    Group.Background,
                    "Backgrounds"
                },
                {
                    Group.None,
                    "Default"
                },
                {
                    Group.Log,
                    "Log"
                },
                {
                    Group.Pings,
                    "Pings"
                },
                {
                    Group.ItemActions,
                    "ItemActions"
                }
            };
            groups = new Dictionary<Group, Dictionary<string, Atlas.Sprite>>();
            Array values = Enum.GetValues(typeof(Group));
            int i = 0;
            for (int length = values.Length; i < length; i++)
            {
                Group group = (Group)values.GetValue(i);
                bool slice9Grid = group == Group.Background;
                Dictionary<string, Atlas.Sprite> dictionary = new Dictionary<string, Atlas.Sprite>(StringComparer.InvariantCultureIgnoreCase);
                groups.Add(group, dictionary);
                if (mapping.TryGetValue(group, out var value))
                {
                    Sprite[] array = Resources.LoadAll<Sprite>("Sprites/" + value);
                    int j = 0;
                    for (int num = array.Length; j < num; j++)
                    {
                        Sprite sprite = array[j];
                        string name = sprite.name;
                        if (!dictionary.ContainsKey(name))
                        {
                            dictionary[name] = new Atlas.Sprite(sprite, slice9Grid);
                            continue;
                        }
                        Debug.LogErrorFormat("Duplicate sprite name {0}", name);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("SpriteManager : Subpath for SpriteManager.G.{0} is not found in mapping Dictionary!", group);
                }
            }
        }

        public static Atlas.Sprite Get(Group group, string name)
        {
            Atlas.Sprite withNoDefault = GetWithNoDefault(group, name);
            if (withNoDefault == null)
            {
                withNoDefault = defaultSprite;
            }
            return withNoDefault;
        }

        private static Atlas.Sprite GetWithNoDefault(Group group, string name)
        {
            Atlas.Sprite sprite;
            if (mapping.TryGetValue(group, out var value))
            {
                sprite = Atlas.GetSprite(value, name);
                if (sprite != null)
                {
                    return sprite;
                }
            }
            sprite = GetFromResources(group, name);
            if (sprite != null)
            {
                return sprite;
            }
            return null;
        }

        public static Atlas.Sprite GetFromResources(Group group, string name)
        {
            if (groups.TryGetValue(group, out var value) && value.TryGetValue(name, out var value2))
            {
                return value2;
            }
            return null;
        }

        public static Atlas.Sprite Get(TechType techType)
        {
            return Get(Group.Item, techType.AsString());
        }

        public static Atlas.Sprite GetWithNoDefault(TechType techType)
        {
            return GetWithNoDefault(Group.Item, techType.AsString());
        }

        public static Atlas.Sprite GetBackground(CraftData.BackgroundType backgroundType)
        {
            return backgroundType switch
            {
                CraftData.BackgroundType.Blueprint => GetFromResources(Group.Background, "Blueprint"), 
                CraftData.BackgroundType.PlantWater => GetFromResources(Group.Background, "PlantWater"), 
                CraftData.BackgroundType.PlantWaterSeed => GetFromResources(Group.Background, "PlantWater"), 
                CraftData.BackgroundType.PlantAir => GetFromResources(Group.Background, "PlantAir"), 
                CraftData.BackgroundType.PlantAirSeed => GetFromResources(Group.Background, "PlantAir"), 
                CraftData.BackgroundType.ExosuitArm => GetFromResources(Group.Background, "ExosuitArm"), 
                _ => GetFromResources(Group.Background, "Normal"), 
            };
        }

        public static Atlas.Sprite GetBackground(TechType techType)
        {
            return GetBackground(CraftData.GetBackgroundType(techType));
        }
    }
}
