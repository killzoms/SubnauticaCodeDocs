namespace AssemblyCSharp
{
    public class VoxelGridRasterizer : VoxelandRasterizer
    {
        private readonly IVoxelGrid grid;

        private readonly Int3 origin;

        public VoxelGridRasterizer(IVoxelGrid grid, Int3 origin)
        {
            this.grid = grid;
            this.origin = origin;
        }

        public void Rasterize(Voxeland land, Array3<byte> windowOut, Array3<byte> densityOut, Int3 size, int wx0, int wy0, int wz0, int downsamples)
        {
            Int3 @int = new Int3(wx0, wy0, wz0) + origin;
            Int3.RangeEnumerator rangeEnumerator = Int3.Range(size);
            while (rangeEnumerator.MoveNext())
            {
                Int3 current = rangeEnumerator.Current;
                Int3 int2 = @int + current;
                if (grid.GetVoxelMask(int2.x, int2.y, int2.z))
                {
                    VoxelandData.OctNode voxel = grid.GetVoxel(int2.x, int2.y, int2.z);
                    windowOut[current.x, current.y, current.z] = voxel.type;
                    densityOut[current.x, current.y, current.z] = voxel.density;
                }
            }
        }

        public bool IsRangeUniform(Int3.Bounds range)
        {
            Int3.RangeEnumerator enumerator = range.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int3 @int = enumerator.Current + origin;
                if (grid.GetVoxelMask(@int.x, @int.y, @int.z) && grid.GetVoxel(@int.x, @int.y, @int.z).type != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsRangeLoaded(Int3.Bounds range, int downsamples)
        {
            return true;
        }

        public void OnPreBuildRange(Int3.Bounds range)
        {
        }

        public void LayoutDebugGUI()
        {
        }
    }
}
