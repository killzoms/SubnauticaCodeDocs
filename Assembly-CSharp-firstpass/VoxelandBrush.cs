using UnityEngine;
using UWE;

[CreateAssetMenu(fileName = "NewVoxelandBrush.asset", menuName = "Voxeland/Create Brush Asset")]
public class VoxelandBrush : ScriptableObject
{
    public Mesh mesh;

    public TextAsset distanceField;

    public float[,,] distanceValues { get; private set; }

    public Bounds bounds { get; private set; }

    public void Load()
    {
        distanceValues = DistanceField.Load(distanceField, out var min, out var max);
        bounds = Utils.MinMaxBounds(min, max);
    }

    public DistanceFieldGrid CreateGrid(Vector3 position, Quaternion rotation, Vector3 scale, byte blockType)
    {
        if (distanceValues == null)
        {
            Load();
        }
        return DistanceFieldGrid.Create(distanceValues, bounds, blockType, position, rotation, scale);
    }
}
