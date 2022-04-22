using System;
using System.Collections;
using ProtoBuf;
using UnityEngine;
using UnityEngine.Events;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    [SkipProtoContractCheck]
    public class StorageContainer : HandTarget, IHandTarget, IConstructable, IObstacle, IProtoTreeEventListener
    {
        [Serializable]
        public class UseEvent : UnityEvent
        {
        }

        private const string legacyStorageName = "StorageContainer Storage";

        [AssertNotNull]
        public GameObject prefabRoot;

        public int width = 6;

        public int height = 8;

        public string hoverText = "OpenStorage";

        public float modelSizeRadius = 1f;

        public string storageLabel = "StorageLabel";

        public UseEvent onUse;

        public FMODAsset errorSound;

        [AssertNotNull]
        public ChildObjectIdentifier storageRoot;

        public bool preventDeconstructionIfNotEmpty;

        private bool open;

        private const int currentVersion = 3;

        [NonSerialized]
        [ProtoMember(1)]
        public int version;

        public ItemsContainer container { get; private set; }

        public override void Awake()
        {
            base.Awake();
            CreateContainer();
        }

        private void CreateContainer()
        {
            if (container == null)
            {
                container = new ItemsContainer(width, height, storageRoot.transform, storageLabel, errorSound);
            }
        }

        public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            version = 3;
        }

        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            CreateContainer();
            StorageHelper.TransferItems(storageRoot.gameObject, container);
            UpgradeLegacyStorage();
        }

        private void UpgradeLegacyStorage()
        {
            if (version < 2)
            {
                bool flag = false;
                StoreInformationIdentifier[] componentsInChildren = prefabRoot.GetComponentsInChildren<StoreInformationIdentifier>(includeInactive: true);
                foreach (StoreInformationIdentifier storeInformationIdentifier in componentsInChildren)
                {
                    if ((bool)storeInformationIdentifier && storeInformationIdentifier.transform.parent == prefabRoot.transform)
                    {
                        if (!flag)
                        {
                            StorageHelper.TransferItems(storeInformationIdentifier.gameObject, container);
                            flag = true;
                        }
                        global::UnityEngine.Object.Destroy(storeInformationIdentifier.gameObject);
                    }
                }
                version = 2;
            }
            else if (version < 3)
            {
                CoroutineHost.StartCoroutine(CleanUpDuplicatedStorage());
            }
        }

        private IEnumerator CleanUpDuplicatedStorage()
        {
            yield return StorageHelper.DestroyDuplicatedItems(prefabRoot);
            version = Mathf.Max(version, 3);
        }

        public void ResetContainer()
        {
            container.Clear();
            Transform transform = storageRoot.transform;
            for (int num = transform.childCount - 1; num >= 0; num--)
            {
                global::UnityEngine.Object.Destroy(transform.GetChild(num).gameObject);
            }
            open = false;
        }

        public bool IsEmpty()
        {
            return container.count <= 0;
        }

        public void OnHandHover(GUIHand hand)
        {
            if (base.enabled)
            {
                Constructable component = base.gameObject.GetComponent<Constructable>();
                if (!component || component.constructed)
                {
                    HandReticle.main.SetInteractText(hoverText, IsEmpty() ? "Empty" : string.Empty);
                    HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                }
            }
        }

        public void OnHandClick(GUIHand guiHand)
        {
            if (base.enabled)
            {
                Constructable component = base.gameObject.GetComponent<Constructable>();
                if (!component || component.constructed)
                {
                    Open();
                    onUse.Invoke();
                }
            }
        }

        public virtual void Open(Transform useTransform)
        {
            PDA pDA = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(container);
            if (pDA.Open(PDATab.Inventory, useTransform, OnClosePDA, modelSizeRadius + 2f))
            {
                open = true;
            }
        }

        public void Open()
        {
            Open(base.transform);
        }

        public bool GetOpen()
        {
            return open;
        }

        protected virtual void OnClose()
        {
        }

        protected void OnClosePDA(PDA pda)
        {
            OnClose();
            onUse.Invoke();
            open = false;
        }

        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;
            if (container == null)
            {
                container = new ItemsContainer(width, height, storageRoot.transform, storageLabel, errorSound);
            }
            else
            {
                container.Resize(width, height);
            }
        }

        public void OnConstructedChanged(bool constructed)
        {
        }

        public bool CanDeconstruct(out string reason)
        {
            if (preventDeconstructionIfNotEmpty && !IsEmpty())
            {
                reason = Language.main.Get("DeconstructNonEmptyStorageContainerError");
                return false;
            }
            reason = null;
            return true;
        }
    }
}
