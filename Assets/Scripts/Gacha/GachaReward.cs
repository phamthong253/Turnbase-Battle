// GachaReward.cs (K?t qu? c?a GachaService.Pull)
public class GachaReward
{
    // Ch? có m?t trong các tr??ng này ???c ?i?n ??y ??
    public UnitSO UnitData;   // Unit m?i nh?n ???c (n?u là l?n ??u)
    public UnitSO ShardData;  // Unit Shard nh?n ???c
    public ItemSO ItemData;   // Item ho?c tài nguyên khác
    public int Quantity;      // S? l??ng nh?n ???c
    public bool IsNewUnit;    // C? hi?u báo Unit này là Unit hoàn toàn m?i
    public bool IsEquipment; // Cho bi?t n?u ph?n th??ng l&agrave; trang b? (d? d&agrave;ng x? l&yacute; sau n&agrave;y)
    public int Crystal;
    public ItemSO itemMaterial;
}