using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float dragSpeed = 2f;
    public float zoomSpeed = 200f;

    private Vector3 dragOrigin;

    void Update()
    {
        HandleKeyboard();
        HandleMouseDrag();
        HandleZoom();
    }

    void HandleKeyboard()
    {
        float h = Input.GetAxis("Horizontal"); // A/D 或 ← →
        float v = Input.GetAxis("Vertical");   // W/S 或 ↑ ↓

        Vector3 move = new Vector3(h, 0, v);
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }

    void HandleMouseDrag()
    {
        // 按下左键开始拖动
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
        }

        if (!Input.GetMouseButton(0)) return;

        Vector3 difference = Input.mousePosition - dragOrigin;
        dragOrigin = Input.mousePosition;

        Vector3 move = new Vector3(-difference.x, 0, -difference.y) * dragSpeed * Time.deltaTime;

        transform.Translate(move, Space.World);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 move = transform.forward * scroll * zoomSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
}