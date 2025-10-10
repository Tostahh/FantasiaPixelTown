using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraPanZoom : MonoBehaviour
{
    [Header("Pan Settings")]
    public float panSpeed = 0.01f;
    public float panSmooth = 10f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;

    [Header("Camera Bounds")]
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Player Follow")]
    public Transform player;
    public float followSmooth = 5f;
    public float zoomOnFollow = 5f;

    private Camera cam;
    private InputSystem_Actions input;

    private bool isPanning = false;
    private Vector2 lastMousePosition;
    private Vector3 targetPosition;

    private bool followPlayer = false;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        input = new InputSystem_Actions();
        targetPosition = transform.position;
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Down.started += OnDownPressed;
        input.Player.Down.canceled += OnDownReleased;

        input.Player.Pan.performed += OnPan;
        input.Player.Zoom.performed += OnZoom;
    }

    private void OnDisable()
    {
        input.Player.Down.started -= OnDownPressed;
        input.Player.Down.canceled -= OnDownReleased;

        input.Player.Pan.performed -= OnPan;
        input.Player.Zoom.performed -= OnZoom;

        input.Disable();
    }

    private void OnDownPressed(InputAction.CallbackContext ctx)
    {
        isPanning = true;
        followPlayer = false; // stop following while manually panning
        lastMousePosition = Mouse.current.position.ReadValue();
    }

    private void OnDownReleased(InputAction.CallbackContext ctx)
    {
        isPanning = false;
    }

    private void OnPan(InputAction.CallbackContext ctx)
    {
        if (!isPanning) return;

        Vector2 currentPos = Mouse.current.position.ReadValue();
        Vector2 delta = currentPos - lastMousePosition;
        lastMousePosition = currentPos;

        Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * panSpeed * cam.orthographicSize;
        targetPosition += move;

        ClampTargetPosition();
    }

    private void OnZoom(InputAction.CallbackContext ctx)
    {
        followPlayer = false; // stop following when zooming manually

        Vector2 scrollDelta = ctx.ReadValue<Vector2>();
        float zoomValue = scrollDelta.y;

        cam.orthographicSize -= zoomValue * zoomSpeed * Time.deltaTime;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        ClampTargetPosition();
    }

    public void StartFollowingPlayer()
    {
        player = FindFirstObjectByType<PlayerController>().transform;
        followPlayer = true;
    }

    private void LateUpdate()
    {
        if (followPlayer && player != null)
        {
            // Smooth follow
            targetPosition = Vector3.Lerp(transform.position, player.position, followSmooth * Time.deltaTime);
            targetPosition.z = transform.position.z;

            // Optionally zoom in on player
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomOnFollow, followSmooth * Time.deltaTime);
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, panSmooth * Time.deltaTime);
        ClampTargetPosition();
    }

    private void ClampTargetPosition()
    {
        if (cam == null) return;

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        float minX = minBounds.x + horzExtent;
        float maxX = maxBounds.x - horzExtent;
        float minY = minBounds.y + vertExtent;
        float maxY = maxBounds.y - vertExtent;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
    }
}
