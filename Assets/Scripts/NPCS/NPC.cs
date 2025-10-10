using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum NPCRole { Citizen, Shopkeeper, Guard, Child }

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class NPC : MonoBehaviour
{
    [Header("Basic Info")]
    public string npcName;
    public NPCRole role;
    public Transform homePosition;

    [Header("Schedule")]
    public List<Schedule> dailySchedule;

    [Header("Movement")]
    public float moveSpeed = 2f;
    private Vector3 currentTarget;
    private List<Vector3> path;
    private int pathIndex;

    [Header("Free Roam")]
    public float freeRoamCooldown = 3f;
    private float freeRoamTimer;

    [Header("Relationships")]
    public List<NPC> friends;

    private DayNightCycle dayNightCycle;
    private TilemapPathfinder pathfinder;

    private Schedule currentSchedule;
    private float scheduleStartTime;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool isGoingHome = false;
    private bool atHome = false;

    private void Awake()
    {
        dayNightCycle = FindFirstObjectByType<DayNightCycle>();
        pathfinder = FindFirstObjectByType<TilemapPathfinder>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        SetInitialPosition();
    }

    private void Update()
    {
        HandleSchedule();
        FollowPath();
        UpdateAnimationAndDirection();
        HandleHomeVisibility();
    }

    void SetInitialPosition()
    {
        currentSchedule = GetActiveSchedule(dayNightCycle.CurrentHour);

        // Spawn at the relevant POI if schedule exists, else near home
        if (currentSchedule != null && currentSchedule.poiType != POIType.None)
        {
            POI poi = POIManager.GetClosestPOI(currentSchedule.poiType, transform.position);
            if (poi != null)
                currentTarget = pathfinder.GetRandomWalkableWorldPositionNear(poi.entryPoint.position, 1);
            else if (homePosition != null)
                currentTarget = pathfinder.GetRandomWalkableWorldPositionNear(homePosition.position, 1);
            else
            {
                homePosition.position = pathfinder.GetRandomWalkableWorldPositionNear(POIManager.GetClosestPOI(POIType.Inn, transform.position).transform.position, 1);
                currentTarget = pathfinder.GetRandomWalkableWorldPositionNear(homePosition.position, 1);
            }
        }
        else
        {
            currentTarget = pathfinder.GetRandomWalkableWorldPositionNear(homePosition.position, 1);
        }

        transform.position = currentTarget;
        path = null;
        pathIndex = 0;
        freeRoamTimer = 0f;
        isGoingHome = false;
        atHome = false;
    }

    Schedule GetActiveSchedule(float hour)
    {
        foreach (Schedule s in dailySchedule)
            if (s.startHour <= hour && hour < s.endHour)
                return s;
        return null;
    }

    void HandleSchedule()
    {
        float hour = dayNightCycle.CurrentHour;
        Schedule activeSchedule = GetActiveSchedule(hour);

        bool shouldGoHome = (hour >= 21f || hour < 6f) ||
                            (activeSchedule != null && !activeSchedule.freeRoam && activeSchedule.poiType == POIType.None) ||
                            (activeSchedule == null);

        if (shouldGoHome)
        {
            if (!isGoingHome && !atHome)
                GoHome();
            currentSchedule = null;
        }
        else
        {
            if (currentSchedule != activeSchedule)
            {
                currentSchedule = activeSchedule;
                scheduleStartTime = hour;
                freeRoamTimer = 0f;
                isGoingHome = false;
                atHome = false;

                // Move toward schedule POI if not free roam
                if (!activeSchedule.freeRoam && activeSchedule.poiType != POIType.None)
                {
                    POI poi = POIManager.GetClosestPOI(activeSchedule.poiType, transform.position);
                    if (poi != null)
                        SetTarget(pathfinder.GetRandomWalkableWorldPositionNear(poi.entryPoint.position, 2));
                }
            }

            // Free roam logic
            if (activeSchedule.freeRoam && pathfinder != null)
            {
                freeRoamTimer -= Time.deltaTime;
                if (Vector3.Distance(transform.position, currentTarget) < 0.1f || freeRoamTimer <= 0f)
                {
                    SetTarget(pathfinder.GetRandomWalkableWorldPosition());
                    freeRoamTimer = freeRoamCooldown;
                }
            }
        }

        // Recalculate path if needed
        if (pathfinder != null && (path == null || pathIndex >= path.Count || Vector3.Distance(currentTarget, path[path.Count - 1]) > 0.1f))
        {
            path = pathfinder.FindPath(transform.position, currentTarget);
            pathIndex = 0;

            if (path == null || path.Count == 0)
                UnstuckNPC();
        }
    }

    void GoHome()
    {
        // Use home POI if available, else random near home
        Vector3 homeTarget = homePosition != null ? pathfinder.GetRandomWalkableWorldPositionNear(homePosition.position, 1) : transform.position;
        if (homeTarget != Vector3.zero)
            SetTarget(homeTarget);

        isGoingHome = true;
        atHome = false;
    }

    void SetTarget(Vector3 newTarget)
    {
        if (currentTarget != newTarget)
        {
            currentTarget = newTarget;
            path = null;
            pathIndex = 0;

            if (!isGoingHome)
            {
                spriteRenderer.enabled = true;
                if (animator != null && !animator.enabled) animator.enabled = true;
            }
        }
    }

    void UnstuckNPC()
    {
        if (pathfinder == null) return;
        Vector3 newTarget = pathfinder.GetClosestWalkableTile(transform.position);
        if (newTarget != Vector3.zero)
            SetTarget(newTarget);
    }

    void FollowPath()
    {
        if (path == null || pathIndex >= path.Count)
        {
            animator.SetBool("Moving", false);
            return;
        }

        Vector3 nextPos = path[pathIndex];
        Vector3 moveDelta = nextPos - transform.position;

        transform.position = Vector3.MoveTowards(transform.position, nextPos, moveSpeed * Time.deltaTime);

        if (moveDelta.x != 0)
            spriteRenderer.flipX = moveDelta.x < 0;

        animator.SetBool("Moving", moveDelta.sqrMagnitude > 0.001f);

        if (Vector3.Distance(transform.position, nextPos) < 0.05f)
        {
            pathIndex++;

            if (isGoingHome && pathIndex >= path.Count)
            {
                atHome = true;
                isGoingHome = false;
            }
        }
    }

    void HandleHomeVisibility()
    {
        if (atHome)
        {
            spriteRenderer.enabled = false;
            if (animator != null) animator.enabled = false;
        }
        else
        {
            spriteRenderer.enabled = true;
            if (animator != null && !animator.enabled) animator.enabled = true;
        }
    }

    void UpdateAnimationAndDirection()
    {
        if (path == null || pathIndex >= path.Count)
            animator.SetBool("Moving", false);
    }
}
