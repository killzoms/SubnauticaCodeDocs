using UnityEngine;

namespace AssemblyCSharp.WorldStreaming
{
    public sealed class ClipmapCell
    {
        private enum State
        {
            Unloaded,
            QueuedForLoading,
            WaitingForOctrees,
            BuildingMesh,
            BuildingLayers,
            Loaded,
            Visible,
            HiddenByParent,
            HiddenByChildren,
            QueuedForUnloading,
            DestroyingChunk,
            BuildingMeshToUnloading,
            BuildingLayersToUnloading
        }

        public readonly ClipmapLevel level;

        private State state;

        private ClipmapChunk chunk;

        public Int3 id { get; private set; }

        public Int3? reloadId { get; private set; }

        private ClipmapStreamer streamer => level.streamer;

        public ClipmapCell(ClipmapLevel level, Int3 id)
        {
            this.level = level;
            this.id = id;
            state = State.Unloaded;
        }

        public void Load()
        {
            reloadId = null;
            switch (state)
            {
            case State.Unloaded:
                state = State.QueuedForLoading;
                level.EnqueueForLoading(this);
                break;
            case State.QueuedForUnloading:
                state = State.Loaded;
                OnLoaded();
                break;
            case State.DestroyingChunk:
                reloadId = id;
                break;
            case State.BuildingMeshToUnloading:
                state = State.BuildingMesh;
                break;
            case State.BuildingLayersToUnloading:
                state = State.BuildingLayers;
                break;
            default:
                Debug.LogErrorFormat("ClipmapCell.Load: Unhandled state {0}, cell {1}", state, this);
                break;
            case State.QueuedForLoading:
            case State.WaitingForOctrees:
            case State.BuildingMesh:
            case State.BuildingLayers:
            case State.Loaded:
            case State.Visible:
            case State.HiddenByParent:
            case State.HiddenByChildren:
                break;
            }
        }

        public void Unload()
        {
            reloadId = null;
            switch (state)
            {
            case State.Loaded:
            case State.HiddenByParent:
            case State.HiddenByChildren:
                state = State.QueuedForUnloading;
                level.EnqueueForUnloading(this);
                break;
            case State.Visible:
                HideMesh();
                state = State.QueuedForUnloading;
                level.EnqueueForVisibilityUpdate(id, loading: false);
                level.EnqueueForUnloading(this);
                break;
            case State.QueuedForLoading:
                state = State.Unloaded;
                break;
            case State.BuildingMesh:
                state = State.BuildingMeshToUnloading;
                break;
            case State.BuildingLayers:
                state = State.BuildingLayersToUnloading;
                break;
            case State.WaitingForOctrees:
                state = State.Unloaded;
                break;
            default:
                Debug.LogErrorFormat("ClipmapCell.Unload: Unhandled state {0}, cell {1}", state, this);
                break;
            case State.Unloaded:
            case State.QueuedForUnloading:
            case State.DestroyingChunk:
            case State.BuildingMeshToUnloading:
            case State.BuildingLayersToUnloading:
                break;
            }
        }

        public void Reload(Int3 newId)
        {
            reloadId = newId;
            switch (state)
            {
            case State.Unloaded:
                id = newId;
                reloadId = null;
                state = State.QueuedForLoading;
                level.EnqueueForLoading(this);
                break;
            case State.QueuedForLoading:
                id = newId;
                reloadId = null;
                break;
            case State.Visible:
                HideMesh();
                state = State.QueuedForUnloading;
                level.EnqueueForVisibilityUpdate(id, loading: false);
                level.EnqueueForUnloading(this);
                break;
            case State.Loaded:
            case State.HiddenByParent:
            case State.HiddenByChildren:
                state = State.QueuedForUnloading;
                level.EnqueueForUnloading(this);
                break;
            case State.BuildingMesh:
                state = State.BuildingMeshToUnloading;
                break;
            case State.BuildingLayers:
                reloadId = newId;
                state = State.BuildingLayersToUnloading;
                break;
            case State.WaitingForOctrees:
                id = newId;
                reloadId = null;
                state = State.QueuedForLoading;
                level.EnqueueForLoading(this);
                break;
            default:
                Debug.LogErrorFormat("ClipmapCell.Reload: Unhandled state {0}, cell {1}", state, this);
                break;
            case State.QueuedForUnloading:
            case State.DestroyingChunk:
            case State.BuildingMeshToUnloading:
            case State.BuildingLayersToUnloading:
                break;
            }
        }

        public bool BeginLoading()
        {
            if (state != State.QueuedForLoading)
            {
                return false;
            }
            state = State.WaitingForOctrees;
            BatchOctreesStreamer octreesStreamer = streamer.host.GetOctreesStreamer(level.id);
            OnBatchOctreesChanged(octreesStreamer);
            return true;
        }

        public void OnBatchOctreesChanged(BatchOctreesStreamer streamer)
        {
            if (state == State.WaitingForOctrees)
            {
                Int3 @int = id * level.cellSize - (3 << level.id);
                Int3 max = @int + level.cellSize + ((6 << level.id) - 1);
                if (streamer.IsRangeLoaded(Int3.MinMax(@int, max)))
                {
                    OnBatchOctreesReady();
                }
            }
        }

        public bool IsProcessing()
        {
            State state = this.state;
            if (state == State.BuildingMesh || state == State.BuildingMeshToUnloading)
            {
                return true;
            }
            return false;
        }

        private bool OnBatchOctreesReady()
        {
            state = State.BuildingMesh;
            streamer.meshingThreads.Enqueue(BeginBuildMesh, this, null);
            return true;
        }

        private static void BeginBuildMesh(object owner, object state)
        {
            ((ClipmapCell)owner).BeginBuildMesh();
        }

        private void BeginBuildMesh()
        {
            MeshBuilder meshBuilder = streamer.meshBuilderPool.Get();
            BatchOctreesStreamer octreesStreamer = streamer.host.GetOctreesStreamer(level.id);
            meshBuilder.Reset(level.id, id, level.cellSize, level.settings, level.streamer.host.blockTypes);
            meshBuilder.DoThreadablePart(octreesStreamer, streamer.settings.collision);
            streamer.streamingThread.Enqueue(EndBuildMesh, this, meshBuilder);
        }

        private static void EndBuildMesh(object owner, object state)
        {
            ClipmapCell obj = (ClipmapCell)owner;
            MeshBuilder meshBuilder = (MeshBuilder)state;
            obj.EndBuildMesh(meshBuilder);
        }

        private void EndBuildMesh(MeshBuilder meshBuilder)
        {
            bool flag = state == State.BuildingMeshToUnloading;
            state = (flag ? State.BuildingLayersToUnloading : State.BuildingLayers);
            streamer.buildLayersThread.Enqueue(BeginBuildLayers, this, meshBuilder);
        }

        private static void BeginBuildLayers(object owner, object state)
        {
            ClipmapCell obj = (ClipmapCell)owner;
            MeshBuilder meshBuilder = (MeshBuilder)state;
            obj.BeginBuildLayers(meshBuilder);
        }

        private void BeginBuildLayers(MeshBuilder meshBuilder)
        {
            WorldStreamer host = streamer.host;
            chunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.chunkPrefab, host.chunkLayerPrefab);
            streamer.meshBuilderPool.Return(meshBuilder);
            streamer.OnCellLoaded(level, id);
            streamer.streamingThread.Enqueue(EndBuildLayers, this, null);
        }

        private static void EndBuildLayers(object owner, object state)
        {
            ((ClipmapCell)owner).EndBuildLayers();
        }

        private void EndBuildLayers()
        {
            if (state == State.BuildingLayersToUnloading)
            {
                state = State.QueuedForUnloading;
                level.EnqueueForUnloading(this);
            }
            else
            {
                state = State.Loaded;
                OnLoaded();
            }
        }

        private void OnLoaded()
        {
            level.EnqueueForVisibilityUpdate(id, loading: true);
        }

        public bool BeginUnloading()
        {
            if (state != State.QueuedForUnloading)
            {
                return false;
            }
            state = State.DestroyingChunk;
            streamer.destroyChunksThread.Enqueue(BeginDestroyChunk, this, null);
            return true;
        }

        private static void BeginDestroyChunk(object owner, object state)
        {
            ((ClipmapCell)owner).BeginDestroyChunk();
        }

        private void BeginDestroyChunk()
        {
            streamer.OnCellUnloaded(level, id);
            if ((bool)chunk)
            {
                MeshBuilder.DestroyMeshes(chunk);
                Object.Destroy(chunk.gameObject);
            }
            streamer.streamingThread.Enqueue(EndDestroyChunk, this, null);
        }

        private static void EndDestroyChunk(object owner, object state)
        {
            ((ClipmapCell)owner).EndDestroyChunk();
        }

        private void EndDestroyChunk()
        {
            state = State.Unloaded;
            if (reloadId.HasValue)
            {
                id = reloadId.Value;
                reloadId = null;
                state = State.QueuedForLoading;
                level.EnqueueForLoading(this);
            }
        }

        private void FadeInMesh()
        {
            streamer.toggleChunksThread.Enqueue(FadeInMesh, this, null);
        }

        private void ShowMesh()
        {
            streamer.toggleChunksThread.Enqueue(ShowMesh, this, null);
        }

        private void HideMesh()
        {
            streamer.toggleChunksThread.Enqueue(HideMesh, this, null);
        }

        private static void FadeInMesh(object owner, object state)
        {
            ClipmapCell clipmapCell = (ClipmapCell)owner;
            ClipmapChunk clipmapChunk = clipmapCell.chunk;
            if ((bool)clipmapChunk)
            {
                clipmapChunk.FadeIn(clipmapCell.level.settings.fadeMeshes);
            }
        }

        private static void ShowMesh(object owner, object state)
        {
            ClipmapChunk clipmapChunk = ((ClipmapCell)owner).chunk;
            if ((bool)clipmapChunk)
            {
                clipmapChunk.Show();
            }
        }

        private static void HideMesh(object owner, object state)
        {
            ClipmapChunk clipmapChunk = ((ClipmapCell)owner).chunk;
            if ((bool)clipmapChunk)
            {
                clipmapChunk.Hide();
            }
        }

        public void Show()
        {
            switch (state)
            {
            case State.Loaded:
                state = State.Visible;
                FadeInMesh();
                break;
            case State.HiddenByParent:
            case State.HiddenByChildren:
                state = State.Visible;
                ShowMesh();
                break;
            case State.Visible:
                break;
            }
        }

        public bool HideByParent()
        {
            switch (state)
            {
            case State.Visible:
                state = State.HiddenByParent;
                HideMesh();
                return false;
            case State.HiddenByParent:
                return false;
            case State.Loaded:
            case State.HiddenByChildren:
                state = State.HiddenByParent;
                return true;
            default:
                return true;
            }
        }

        public bool HideByChildren()
        {
            switch (state)
            {
            case State.Visible:
                state = State.HiddenByChildren;
                HideMesh();
                return true;
            case State.HiddenByChildren:
                return false;
            case State.Loaded:
            case State.HiddenByParent:
                state = State.HiddenByChildren;
                return true;
            default:
                return true;
            }
        }

        public bool IsLoaded()
        {
            State state = this.state;
            if ((uint)(state - 5) <= 3u)
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"ClipmapCell (level {level}, id {id}, state {state}, reloadId {reloadId})";
        }

        public void DrawGizmos()
        {
            Color color = Color.white;
            switch (state)
            {
            case State.Unloaded:
                color = Color.black;
                break;
            case State.QueuedForLoading:
            case State.WaitingForOctrees:
                color = Color.red;
                break;
            case State.BuildingMesh:
            case State.BuildingLayers:
            case State.BuildingMeshToUnloading:
            case State.BuildingLayersToUnloading:
                color = Color.yellow;
                break;
            case State.Loaded:
                color = Color.blue;
                break;
            case State.Visible:
                color = Color.green;
                break;
            case State.HiddenByParent:
            case State.HiddenByChildren:
                color = Color.gray;
                break;
            case State.QueuedForUnloading:
            case State.DestroyingChunk:
                color = Color.cyan;
                break;
            }
            Gizmos.color = color;
            int cellSize = level.cellSize;
            Gizmos.DrawWireCube(size: (Vector3)new Int3(cellSize - 4 + level.id), center: (Vector3)(id * cellSize + cellSize / 2));
        }

        public static bool IsLoaded(ClipmapCell cell)
        {
            return cell?.IsLoaded() ?? false;
        }

        public static Int3.Bounds GetChildIds(Int3 cellId)
        {
            Int3 @int = cellId << 1;
            Int3 max = @int + 1;
            return Int3.MinMax(@int, max);
        }
    }
}
