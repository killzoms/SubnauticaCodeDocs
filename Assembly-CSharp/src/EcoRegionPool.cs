using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class EcoRegionPool
    {
        private const int initNodeCount = 8000;

        private ObjectPool<EcoRegion> pooledRegions = ObjectPoolHelper.CreatePool<EcoRegion>(1024);

        private const float regionTimeout = 15f;

        private LinkedList<EcoRegion> regionsInUse = new LinkedList<EcoRegion>();

        private Stack<LinkedListNode<EcoRegion>> nodePool = new Stack<LinkedListNode<EcoRegion>>(8000);

        private LinkedListNode<EcoRegion> checkNode;

        public EcoRegionPool()
        {
            for (int i = 0; i < 8000; i++)
            {
                LinkedListNode<EcoRegion> item = new LinkedListNode<EcoRegion>(null);
                nodePool.Push(item);
            }
        }

        private void AddToInUseList(EcoRegion region)
        {
            if (nodePool.Count == 0)
            {
                LinkedListNode<EcoRegion> item = new LinkedListNode<EcoRegion>(null);
                nodePool.Push(item);
            }
            region.timeStamp = Time.time;
            LinkedListNode<EcoRegion> linkedListNode = nodePool.Pop();
            linkedListNode.Value = region;
            regionsInUse.AddLast(linkedListNode);
        }

        public EcoRegion Get()
        {
            EcoRegion ecoRegion = pooledRegions.Get();
            AddToInUseList(ecoRegion);
            return ecoRegion;
        }

        private void Return(EcoRegion region)
        {
            pooledRegions.Return(region);
        }

        private void Return(LinkedListNode<EcoRegion> regionNode)
        {
            EcoRegion value = regionNode.Value;
            pooledRegions.Return(value);
            regionsInUse.Remove(regionNode);
            regionNode.Value = null;
            nodePool.Push(regionNode);
        }

        public void Update()
        {
            if (checkNode == null)
            {
                checkNode = regionsInUse.First;
            }
            if (checkNode != null)
            {
                LinkedListNode<EcoRegion> next = checkNode.Next;
                EcoRegion value = checkNode.Value;
                if (value.Empty() && Time.time - value.timeStamp > 15f)
                {
                    Return(checkNode);
                }
                checkNode = next;
            }
        }
    }
}
