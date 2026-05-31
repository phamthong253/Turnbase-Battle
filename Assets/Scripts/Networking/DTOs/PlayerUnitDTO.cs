using System;

[Serializable]
public class PlayerUnitDTO
{
    // Tên biến phải viết thường chữ cái đầu để khớp với JSON của ASP.NET
    public string unitID;
    public int level;
    public int rank;
    public int currentExp;
    public bool[] isEquipped;
}