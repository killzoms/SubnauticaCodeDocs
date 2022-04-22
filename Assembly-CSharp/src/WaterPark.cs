using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class WaterPark : MonoBehaviour, IBaseModule, IProtoEventListener
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version;

        [NonSerialized]
        [ProtoMember(2)]
        public float _constructed = 1f;

        [NonSerialized]
        [ProtoMember(3)]
        public Base.Face _moduleFace;

        private int height;

        private bool isDirty;

        public Transform itemsRoot;

        public Planter planter;

        public double spreadInfectionInterval = 60.0;

        private List<WaterParkItem> items = new List<WaterParkItem>();

        private int wpPieceCapacity = 10;

        private int usedSpace;

        private double timeNextInfectionSpread = -1.0;

        public const float externalRadius = 2.8f;

        public const float internalRadius = 2.2f;

        public Base.Face moduleFace
        {
            get
            {
                return _moduleFace;
            }
            set
            {
                _moduleFace = value;
            }
        }

        public float constructed
        {
            get
            {
                return _constructed;
            }
            set
            {
                value = Mathf.Clamp01(value);
                if (_constructed != value)
                {
                    _constructed = value;
                    if (!(_constructed >= 1f) && _constructed <= 0f)
                    {
                        global::UnityEngine.Object.Destroy(base.gameObject);
                    }
                }
            }
        }

        private void OnGlobalEntitiesLoaded()
        {
            foreach (Transform item in itemsRoot.transform)
            {
                AddItem(item.GetComponent<Pickupable>());
            }
        }

        private void Update()
        {
            if (isDirty)
            {
                isDirty = false;
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].ValidatePosition();
                }
                planter.SetMaxPlantsHeight(Base.cellSize.y * ((float)height - 0.5f));
            }
            double timePassed = DayNightCycle.main.timePassed;
            if (timeNextInfectionSpread > 0.0 && timePassed > timeNextInfectionSpread)
            {
                if (InfectCreature())
                {
                    timeNextInfectionSpread = timePassed + spreadInfectionInterval;
                }
                else
                {
                    timeNextInfectionSpread = -1.0;
                }
            }
        }

        public static void TransferValue(WaterPark srcWaterPark, WaterPark dstWaterPark)
        {
            List<WaterParkItem> list = new List<WaterParkItem>(srcWaterPark.items);
            for (int i = 0; i < list.Count; i++)
            {
                srcWaterPark.MoveItemTo(list[i], dstWaterPark);
            }
        }

        public static void Unite(WaterPark bottomWaterPark, WaterPark topWaterPark)
        {
            TransferValue(topWaterPark, bottomWaterPark);
            bottomWaterPark.height += topWaterPark.height;
            global::UnityEngine.Object.Destroy(topWaterPark.gameObject);
        }

        public static void Split(WaterPark bottomWaterPark, WaterPark topWaterPark)
        {
            float num = Base.cellSize.y * (float)bottomWaterPark.height;
            List<WaterParkItem> list = new List<WaterParkItem>();
            for (int i = 0; i < bottomWaterPark.items.Count; i++)
            {
                WaterParkItem waterParkItem = bottomWaterPark.items[i];
                if (waterParkItem.transform.localPosition.y > num)
                {
                    list.Add(waterParkItem);
                }
            }
            for (int j = 0; j < list.Count; j++)
            {
                bottomWaterPark.MoveItemTo(list[j], topWaterPark);
            }
        }

        public void Rebuild(Base hostBase, Int3 cell)
        {
            Int3 @int = new Int3(0, 1, 0);
            if (isDirty)
            {
                return;
            }
            isDirty = true;
            if (height == 0)
            {
                WaterPark waterParkModule = GetWaterParkModule(hostBase, cell - @int);
                if (waterParkModule != null && waterParkModule.height > 1)
                {
                    TransferValue(waterParkModule, this);
                    height = waterParkModule.height - 1;
                    waterParkModule.height = 1;
                }
            }
            int num = 0;
            Int3 cell2 = cell;
            WaterPark waterPark = null;
            do
            {
                num++;
                cell2 += @int;
            }
            while (IsCellContainWaterPark(hostBase, cell2) && (waterPark = GetWaterParkModule(hostBase, cell2)) == null);
            int num2 = height;
            height = num;
            if (waterPark != null)
            {
                waterPark.Rebuild(hostBase, cell2);
                Unite(this, waterPark);
            }
            else if (height < num2)
            {
                cell2 += @int;
                if (IsCellContainWaterPark(hostBase, cell2))
                {
                    waterPark = GetWaterParkModule(hostBase, cell2, spawnIfNull: true);
                    Split(this, waterPark);
                }
            }
        }

        public static WaterPark GetWaterParkModule(Base hostBase, Int3 cell, bool spawnIfNull = false)
        {
            WaterPark waterPark = hostBase.GetModule(new Base.Face(cell, Base.Direction.Below)) as WaterPark;
            if (spawnIfNull && waterPark == null)
            {
                waterPark = Spawn(hostBase, cell);
            }
            return waterPark;
        }

        private static WaterPark Spawn(Base hostBase, Int3 cell)
        {
            GameObject prefabForFilename = PrefabDatabase.GetPrefabForFilename("Submarine/Build/WaterPark");
            hostBase.SpawnModule(prefabForFilename, new Base.Face(cell, Base.Direction.Below));
            return GetWaterParkModule(hostBase, cell);
        }

        private static bool IsCellContainWaterPark(Base hostBase, Int3 cell)
        {
            return hostBase.GetFace(new Base.Face(cell, Base.Direction.Below)) == Base.FaceType.WaterPark;
        }

        public static bool CanDropItemInside(Pickupable item)
        {
            if (item == null)
            {
                return false;
            }
            GameObject gameObject = item.gameObject;
            if (gameObject.GetComponent<Creature>() == null && gameObject.GetComponent<CreatureEgg>() == null)
            {
                return false;
            }
            LiveMixin component = gameObject.GetComponent<LiveMixin>();
            if (!(component == null))
            {
                return component.IsAlive();
            }
            return true;
        }

        public void AddItem(Pickupable pickupable)
        {
            GameObject gameObject = pickupable.gameObject;
            WaterParkItem waterParkItem = ((!(gameObject.GetComponent<Creature>() != null)) ? gameObject.EnsureComponent<WaterParkItem>() : gameObject.EnsureComponent<WaterParkCreature>());
            waterParkItem.pickupable = pickupable;
            waterParkItem.infectedMixin = waterParkItem.GetComponent<InfectedMixin>();
            AddItem(waterParkItem);
        }

        public void RemoveItem(Pickupable pickupable)
        {
            WaterParkItem component = pickupable.GetComponent<WaterParkItem>();
            if (component != null)
            {
                RemoveItem(component);
            }
        }

        public void AddItem(WaterParkItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
                usedSpace += item.GetSize();
                item.enabled = true;
                item.transform.parent = itemsRoot;
                item.SetWaterPark(this);
                UpdateInfectionSpreading();
            }
        }

        public void RemoveItem(WaterParkItem item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                usedSpace -= item.GetSize();
                if (item.transform.parent == itemsRoot)
                {
                    item.transform.parent = null;
                }
                if (item.GetWaterPark() == this)
                {
                    item.SetWaterPark(null);
                }
                UpdateInfectionSpreading();
            }
        }

        private void MoveItemTo(WaterParkItem item, WaterPark waterPark)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                usedSpace -= item.GetSize();
                waterPark.AddItem(item);
            }
        }

        public Vector3 GetRandomPointInside()
        {
            Vector2 vector = global::UnityEngine.Random.insideUnitCircle * 2.2f;
            float max = Base.cellSize.y * (float)height - 2.2f;
            float y = global::UnityEngine.Random.Range(-0.3f, max);
            return base.transform.position + new Vector3(vector.x, y, vector.y);
        }

        public void EnsurePointIsInside(ref Vector3 point)
        {
            Vector3 localPoint = base.transform.InverseTransformPoint(point);
            EnsureLocalPointIsInside(ref localPoint);
            point = base.transform.TransformPoint(localPoint);
        }

        public void EnsureLocalPointIsInside(ref Vector3 localPoint)
        {
            float num = Base.cellSize.y * (float)height - 2.2f;
            if (localPoint.y < 0f)
            {
                localPoint.y = 0f;
            }
            else if (localPoint.y > num)
            {
                localPoint.y = num;
            }
            Vector3 vector = new Vector3(localPoint.x, 0f, localPoint.z);
            if (vector.magnitude > 2.2f)
            {
                vector = 2.2f * vector.normalized;
                localPoint.x = vector.x;
                localPoint.z = vector.z;
            }
        }

        public bool IsPointInside(Vector3 point)
        {
            Vector3 vector = base.transform.InverseTransformPoint(point);
            if (vector.y < 0f - Base.halfCellSize.y || vector.y > 0f - Base.halfCellSize.y + Base.cellSize.y * (float)height)
            {
                return false;
            }
            vector.y = 0f;
            return vector.magnitude < 2.8f;
        }

        public bool HasFreeSpace()
        {
            return wpPieceCapacity * height > usedSpace;
        }

        public void TryBreed(WaterParkCreature creature)
        {
            if (items.Contains(creature) && HasFreeSpace())
            {
                TechType techType = creature.pickupable.GetTechType();
                WaterParkCreature waterParkCreature = items.Find((WaterParkItem item) => item != creature && item is WaterParkCreature && (item as WaterParkCreature).GetCanBreed() && item.pickupable != null && item.pickupable.GetTechType() == techType) as WaterParkCreature;
                if (!(waterParkCreature == null))
                {
                    waterParkCreature.ResetBreedTime();
                    WaterParkCreature.Born(WaterParkCreature.creatureEggs.GetOrDefault(techType, techType), this, creature.transform.position + Vector3.down);
                }
            }
        }

        private bool ContainsHeroPeepers()
        {
            for (int i = 0; i < items.Count; i++)
            {
                Peeper component = items[i].GetComponent<Peeper>();
                if (component != null && component.isHero)
                {
                    return true;
                }
            }
            return false;
        }

        private bool ContainsInfectedCreature()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].infectedMixin != null && items[i].infectedMixin.GetInfectedAmount() > 0.25f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool InfectCreature()
        {
            bool result = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].infectedMixin != null && items[i].infectedMixin.GetInfectedAmount() < 1f)
                {
                    items[i].infectedMixin.SetInfectedAmount(1f);
                    result = true;
                    break;
                }
            }
            return result;
        }

        private void CureAllCreatures()
        {
            InfectedMixin infectedMixin = null;
            for (int i = 0; i < items.Count; i++)
            {
                infectedMixin = items[i].infectedMixin;
                if (infectedMixin != null && infectedMixin.GetInfectedAmount() > 0.1f)
                {
                    infectedMixin.SetInfectedAmount(0.1f);
                }
            }
        }

        private void UpdateInfectionSpreading()
        {
            if (ContainsHeroPeepers())
            {
                CureAllCreatures();
                timeNextInfectionSpread = -1.0;
            }
            else if (timeNextInfectionSpread < 0.0 && ContainsInfectedCreature())
            {
                timeNextInfectionSpread = DayNightCycle.main.timePassed + spreadInfectionInterval;
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            version = 1;
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            if (version >= 1)
            {
                return;
            }
            version = 1;
            Constructable component = GetComponent<Constructable>();
            if (component != null)
            {
                constructed = component.amount;
                global::UnityEngine.Object.Destroy(component);
            }
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null)
            {
                Int3 cell = componentInParent.WorldToGrid(base.transform.position);
                Base.Face face = new Base.Face(cell, Base.Direction.Below);
                if (componentInParent.GetFaceRaw(face) == Base.FaceType.WaterPark)
                {
                    face.cell -= componentInParent.GetAnchor();
                    _moduleFace = face;
                    return;
                }
            }
            Debug.LogError("Failed to upgrade savegame data. FiltrationMachine IBaseModule is not found", this);
        }
    }
}
