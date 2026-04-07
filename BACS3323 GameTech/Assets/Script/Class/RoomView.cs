using UnityEngine;

public class RoomView : MonoBehaviour
{
    public GameObject wallUp;
    public GameObject wallDown;
    public GameObject wallLeft;
    public GameObject wallRight;

    public void SetRoom(RoomData data)
    {
        wallUp.SetActive(!data.up);
        wallDown.SetActive(!data.down);
        wallLeft.SetActive(!data.left);
        wallRight.SetActive(!data.right);
    }
}
