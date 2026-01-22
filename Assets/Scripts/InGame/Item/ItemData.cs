using UnityEngine;

public enum ItemId
{
    NONE = 0,

    RM_001_SCRAP_PLATE,
    RM_002_STEEL_BEAM,
    RM_003_TOOLBOX_PARTS,
    RM_004_WIRE_BUNDLE,
    RM_005_POWER_CELL,
    RM_006_MECH_PARTS,
    RM_007_CONTROL_MODULE,
    RM_008_CIRCUIT_BOARD,
    RM_009_FUEL_CANISTER,
}
[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public ItemId id = ItemId.NONE;
    public string itemName;
    public Sprite sprite;
    [Range(1, 1000)] public int weight = 100;
    public float spawnRadius = 1.2f;
    public float repairPoint = 1;
}
