using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    private TilemapPathfinder pathfinder;
    private List<Vector3> path;
    private int pathIndex;
    private Vector3 currentTarget;

    [Header("Input")]
    private InputSystem_Actions input;

    [Header("Animation & Sprite")]
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [Header("Building Placement")]
    public BuildingManager buildingManager; // reference to check if placing

    private void Awake()
    {
        buildingManager = FindFirstObjectByType<BuildingManager>();
        pathfinder = FindFirstObjectByType<TilemapPathfinder>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        input = new InputSystem_Actions();
        input.Enable();
        input.Player.Move.performed += OnMoveClicked;
    }

    private void OnDestroy()
    {
        input.Player.Move.performed -= OnMoveClicked;
    }

    private void OnMoveClicked(InputAction.CallbackContext context)
    {
        // Ignore clicks if over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Ignore clicks if placing a building
        if (buildingManager != null && buildingManager.IsPlacingBuilding)
            return;

        // Get mouse world position
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        // Check if target is walkable
        if (pathfinder.IsWalkable(worldPos))
        {
            path = pathfinder.FindPath(transform.position, worldPos);
            if (path != null && path.Count > 0)
            {
                pathIndex = 0;
                currentTarget = path[pathIndex];
                Camera.main.GetComponent<CameraPanZoom>()?.StartFollowingPlayer();
            }
        }
    }

    private void Update()
    {
        FollowPath();
        UpdateAnimation();
    }

    private void FollowPath()
    {
        if (path == null || pathIndex >= path.Count)
        {
            path = null;
            return;
        }

        Vector3 nextPos = path[pathIndex];
        transform.position = Vector3.MoveTowards(transform.position, nextPos, moveSpeed * Time.deltaTime);

        // Flip sprite based on horizontal movement
        if (nextPos.x > transform.position.x + 0.01f)
            spriteRenderer.flipX = false;
        else if (nextPos.x < transform.position.x - 0.01f)
            spriteRenderer.flipX = true;

        if (Vector3.Distance(transform.position, nextPos) < 0.05f)
        {
            pathIndex++;
            if (pathIndex < path.Count)
                currentTarget = path[pathIndex];
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = path != null && pathIndex < path.Count;
        animator.SetBool("Moving", isMoving);
    }
}
