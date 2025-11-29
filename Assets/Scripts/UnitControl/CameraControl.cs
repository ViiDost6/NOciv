using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 2f;
    public float zoomMin = 5f;
    private float zoomMax;
    
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;

    private float currentMinX;
    private float currentMaxX;
    private float currentMinY;
    private float currentMaxY;

    private Camera cam;
    private MapGenerator mapGenerator;

    void Start()
    {
        cam = Camera.main;
        mapGenerator = FindFirstObjectByType<MapGenerator>();

        minX = -0.85f;
        maxX = mapGenerator.mapHeight * 0.85f;
        minY = -1f;
        maxY = mapGenerator.mapWidth + 0.5f;

        zoomMax = Mathf.Min(maxX / 2f, maxY / 2f) / 1.7f;

        UpdateCameraCurrentBorders();
        ClampBorders();
    }

    void Update()
    {
        MoveCamera();
        ZoomControl();
    }

    private void MoveCamera()
    {
        float movX = Input.GetAxis("Horizontal");
        float movY = Input.GetAxis("Vertical");

        Vector3 delta = new Vector3(movX, movY, 0f) * panSpeed * Time.deltaTime;
        Vector3 proposedPos = transform.position + delta;

        if (proposedPos.x < minX || proposedPos.x > maxX) delta.x = 0f;
        if (proposedPos.y < minY || proposedPos.y > maxY) delta.y = 0f;
        
        transform.position += delta;

        UpdateCameraCurrentBorders();
        ClampBorders();
    }

    private void ZoomControl()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        cam.orthographicSize -= scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, zoomMin, zoomMax);

        UpdateCameraCurrentBorders();
        ClampBorders();
    }

    private void UpdateCameraCurrentBorders()
    {
        float camHeight = cam.orthographicSize;
        float camWidth = cam.orthographicSize * cam.aspect;

        currentMinX = transform.position.x - camWidth;
        currentMaxX = transform.position.x + camWidth;
        currentMinY = transform.position.y - camHeight;
        currentMaxY = transform.position.y + camHeight;
    }

    private void ClampBorders()
    {
        Vector3 pos = transform.position;

        float camHeight = cam.orthographicSize;
        float camWidth = cam.orthographicSize * cam.aspect;

        if(currentMinX < minX) pos.x = minX + camWidth;
        if(currentMaxX > maxX) pos.x = maxX - camWidth;
        if(currentMinY < minY) pos.y = minY + camHeight;
        if(currentMaxY > maxY) pos.y = maxY - camHeight;

        transform.position = pos;

        UpdateCameraCurrentBorders();
    }
}