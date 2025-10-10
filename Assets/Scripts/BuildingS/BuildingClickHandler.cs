using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;

public class BuildingClickHandler : MonoBehaviour
{
    private Camera mainCam;
    public BuildingPopupUI popupUI;

    private InputSystem_Actions input;
    private BuildingManager buildingManager;
    private bool clickQueued;

    // Gizmo variables
    private Vector3 gizmoPoint;
    private bool hasGizmoToDraw;

    private void Awake()
    {
        mainCam = Camera.main;
        input = new InputSystem_Actions();
        buildingManager = FindFirstObjectByType<BuildingManager>();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Player.Move.performed += OnClick;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnClick;
        input.Disable();
    }
    private void OnClick(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = Pointer.current.position.ReadValue();

        // Skip if over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Skip if placing building
        if (buildingManager != null && buildingManager.IsPlacingBuilding)
            return;

        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;

        gizmoPoint = worldPos;
        hasGizmoToDraw = true;

        // Get all colliders at click point
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        bool buildingFound = false;
        bool rubbleFound = false;

        foreach (var hit in hits)
        {
            if (!rubbleFound && hit.TryGetComponent(out CollectibleRubble rubble))
            {
                rubble.Collect();
                rubbleFound = true;
                Debug.Log("RubbleCollected");
            }

            if (!buildingFound && hit.TryGetComponent(out Building building))
            {
                popupUI.Show(building);
                buildingFound = true;
                Debug.Log("BuildingSelected");
            }
        }

        if (!buildingFound && !rubbleFound)
            Debug.Log("Miss");
    }


    private void OnDrawGizmos()
    {
        if (!hasGizmoToDraw) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(gizmoPoint, 0.1f);
    }
}
