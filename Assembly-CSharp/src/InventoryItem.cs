using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class InventoryItem
    {
        public bool ignoreForSorting;

        public IItemsContainer container;

        public bool isEnabled = true;

        private int _x = -1;

        private int _y = -1;

        private int ghostWidth = 1;

        private int ghostHeight = 1;

        public int x => _x;

        public int y => _y;

        public int width
        {
            get
            {
                if (!(item == null))
                {
                    return CraftData.GetItemSize(item.GetTechType()).x;
                }
                return ghostWidth;
            }
        }

        public int height
        {
            get
            {
                if (!(item == null))
                {
                    return CraftData.GetItemSize(item.GetTechType()).y;
                }
                return ghostHeight;
            }
        }

        public bool isBindable => CraftData.GetEquipmentType(item.GetTechType()) == EquipmentType.Hand;

        public Pickupable item { get; private set; }

        public InventoryItem(Pickupable pickupable)
        {
            if (pickupable != null)
            {
                item = pickupable;
                item.SetInventoryItem(this);
            }
            else
            {
                Debug.LogException(new Exception("Attempt to initialize InventoryItem instance with null Pickupable object!"));
            }
        }

        public InventoryItem(int w, int h)
        {
            item = null;
            ghostWidth = w;
            ghostHeight = h;
        }

        public void SetGhostDims(int w, int h)
        {
            ghostWidth = w;
            ghostHeight = h;
        }

        public void SetPosition(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public bool CanDrag(bool verbose)
        {
            if (item != null)
            {
                return container.AllowedToRemove(item, verbose);
            }
            return false;
        }
    }
}
