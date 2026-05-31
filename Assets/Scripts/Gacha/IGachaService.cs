using System.Collections.Generic;
using UnityEngine;

public interface IGachaService
{
    List<GachaReward> Roll(GachaCostSO costData); // Hàm thực hiện roll
}
