using UnityEngine;

[CreateAssetMenu(menuName = "WFC/Room Tile")]
public class WFCTile : ScriptableObject
{
    public string tileName;
    public GameObject prefab;

    // 基础连接性（初始状态，比如 0 度旋转时）
    public bool up, down, left, right;

    [Range(0f, 1f)] public float weight = 1f;

    // 获取旋转后的连接性数据
    // rotationIndex: 0=0°, 1=90°, 2=180°, 3=270°
    public Connectivity GetRotatedConnectivity(int rotationIndex)
    {
        Connectivity c = new Connectivity { up = up, down = down, left = left, right = right };
        for (int i = 0; i < rotationIndex; i++)
        {
            bool oldUp = c.up;
            c.up = c.left;    // 顺时针旋转
            c.left = c.down;
            c.down = c.right;
            c.right = oldUp;
        }
        return c;
    }

}
