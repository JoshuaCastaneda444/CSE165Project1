using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ProjectOneRightController : MonoBehaviour
{
    public XRRayInteractor RayInteractor;
    public ActionBasedController RightControllerInput;
    private Vector3 RightControllerVelocity;
    private Vector3 RightControllerPreviousVelocity;
    private Vector3 RightControllerAcceleration;
    private Vector3 RightControllerPreviousPosition;

    // Colors
    public Material DefaultMaterial;
    public Material HoverMaterial;
    public Material SelectedMaterial;

    // Selecting Objects
    private List<CSE165Interactable> SelectedObjects = new();
    private CSE165Interactable LastHoveredObject;
    public GameObject AreaIndicator;
    public Quaternion AreaIndicatorOriginalRotation;

    // Spawning Objects
    public GameObject[] SpawnObjects;
    private int SpawnObjectIndex = 0;
    private GameObject LastSpawnedObject;
    private Vector3 ControllerPositionAtLastSpawn;
    private Vector3 SpawnedObjectPositionAtLastSpawn;

    // Cycling Object Spawned
    private float LastShakeTime = 0.0f;
    private float ShakeCooldown = 1.5f;

    // Translating

    // Right Controller Actions
    enum Action {Spawn, Translate, Rotate, Scale, Duplicate};

    private Action CurrentAction = Action.Spawn;

    private bool ActionWasHeld = false;
    private bool SelectWasHeld = false;
    private float SelectTimer = 0.0f;
    private bool AreaSelected = false;

    private void Start()
    {
        AreaIndicatorOriginalRotation = AreaIndicator.transform.rotation;
    }

    private void Update()
    {
        // Manually calculate controller velocity/acceleration because unity xr stuff is being dumb
        RightControllerPreviousVelocity = RightControllerVelocity;
        RightControllerVelocity = (transform.position - RightControllerPreviousPosition) / Time.deltaTime;
        RightControllerPreviousPosition = transform.position;
        RightControllerAcceleration = RightControllerVelocity - RightControllerPreviousVelocity;

        // Lock AreaIndicator to controller 
        AreaIndicator.transform.position = new Vector3(transform.position.x, 0.075f, transform.position.z);
        AreaIndicator.transform.rotation = AreaIndicatorOriginalRotation;

        // Handle inputs
        if (RightControllerInput.selectAction.action.IsPressed())
        {
            if (!SelectWasHeld)
            {
                SelectWasHeld = true;
                OnSelect();
            }
            else
            {
                if ((SelectTimer >= 0.4f && SelectTimer <= 1.5f) || (AreaSelected && SelectTimer <= 2.0f))
                {
                    AreaIndicator.SetActive(true);
                }
                else {
                    AreaIndicator.SetActive(false);
                }

                if (SelectTimer >= 1.5f && !AreaSelected)
                {
                    bool allSelected = true;

                    // Select all objects within distance
                    foreach (Collider collider in Physics.OverlapSphere(AreaIndicator.transform.position, AreaIndicator.transform.localScale.x / 2.0f))
                    {
                        CSE165Interactable interactable;
                        if (interactable = collider.gameObject.GetComponent<CSE165Interactable>())
                        {
                            if (!SelectedObjects.Contains(interactable)) allSelected = false;
                        }
                    }

                    foreach (Collider collider in Physics.OverlapSphere(AreaIndicator.transform.position, AreaIndicator.transform.localScale.x / 2.0f))
                    {
                        CSE165Interactable interactable;
                        if (interactable = collider.gameObject.GetComponent<CSE165Interactable>())
                        {
                            if (allSelected)
                            {
                                interactable.Deselect();
                                SelectedObjects.Remove(interactable);
                            }
                            else
                            {
                                if (!SelectedObjects.Contains(interactable))
                                {
                                    interactable.Select();
                                    SelectedObjects.Add(interactable);
                                }
                            }
                        }
                    }

                    AreaSelected = true;
                }
            }

            SelectTimer += Time.deltaTime;
        } else
        {
            SelectWasHeld = false;
            SelectTimer = 0.0f;
            AreaSelected = false;
            AreaIndicator.SetActive(false);
        }

        if (RightControllerInput.activateAction.action.IsPressed())
        {
            if (!ActionWasHeld)
            {
                ActionWasHeld = true;
                OnAction();
            }
            else
            {
                OnActionHold();
            }
            
        } else
        {
            ActionWasHeld = false;

            // TODO! Keep velocity that it had while being held
            // if (LastSpawnedObject)
            // {
            //     Rigidbody LastSpawnedObjectRigidBody = LastSpawnedObject.GetComponent<Rigidbody>();

            //     if (LastSpawnedObjectRigidBody)
            //     {
            //         LastSpawnedObjectRigidBody.velocity = RightControllerVelocity;
            //     }
            // }

            LastSpawnedObject = null;
            ControllerPositionAtLastSpawn = Vector3.zero;
            SpawnedObjectPositionAtLastSpawn = Vector3.zero;
        }

        // Detect Shaking
        if (RightControllerAcceleration.magnitude >= 2.2f && (Time.time - LastShakeTime >= ShakeCooldown))
        {
            LastShakeTime = Time.time;
            CycleSpawnObject();
        }

        // Handle Right Controller as Pointer
        if (RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject HoveredObject = hit.collider.gameObject;
            CSE165Interactable interactable;
            if (interactable = HoveredObject.GetComponent<CSE165Interactable>())
            {
                if (HoveredObject != LastHoveredObject)
                {
                    if (LastHoveredObject) LastHoveredObject.Unhighlight();
                    LastHoveredObject = null;

                    interactable.Highlight();
                    LastHoveredObject = interactable;
                }
            }
            else
            {
                if (LastHoveredObject) LastHoveredObject.Unhighlight();
                LastHoveredObject = null;
            }
        }
        else
        {
            if (LastHoveredObject) LastHoveredObject.Unhighlight();
            LastHoveredObject = null;
        }
    }

    private void OnAction()
    {
        switch (CurrentAction)
        {
            case Action.Spawn:
            {
                SpawnAction();
                break;
            }
            case Action.Translate:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.TranslateAction(transform.position);
                }
                break;
            }
            case Action.Rotate:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.RotateAction();
                }
                break;
            }
            case Action.Scale:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.ScaleAction(transform.position);
                }
                break;
            }
            case Action.Duplicate:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.DuplicateAction(transform.position);
                }
                break;
            }
        }
    }

    private void OnActionHold()
    {
        switch (CurrentAction)
        {
            case Action.Spawn:
            {
                SpawnHold();
                break;
            }
            case Action.Translate:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.TranslateHold(transform.position);
                }
                break;
            }
            case Action.Rotate:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.RotateHold(transform.rotation);
                }
                break;
            }
            case Action.Scale:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.ScaleHold(transform.position);
                }
                break;
            }
            case Action.Duplicate:
            {
                foreach (CSE165Interactable interactable in SelectedObjects)
                {
                    interactable.DuplicateHold();
                }
                break;
            }
        }
    }

    private void OnSelect()
    {
        if (RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject HoveredObject = hit.collider.gameObject;
            CSE165Interactable interactable;
            if (interactable = HoveredObject.GetComponent<CSE165Interactable>())
            {
                ToggleSelect(interactable);
            }
        }
    }

    private void ToggleSelect(CSE165Interactable SelectedObject)
    {
        if (SelectedObjects.Contains(SelectedObject))
        {
            // Deselect
            SelectedObjects.Remove(SelectedObject);
            LastHoveredObject.Deselect();
        }
        else
        {
            // Select
            SelectedObjects.Add(SelectedObject);
            LastHoveredObject.Select();
        }
    }

    private void CycleSpawnObject()
    {
        SpawnObjectIndex++;

        if (SpawnObjectIndex >= SpawnObjects.Length)
        {
            SpawnObjectIndex = 0;
        }
    }

    public void CycleAction()
    {
        switch (CurrentAction)
        {
            case Action.Spawn:
            {
                CurrentAction = Action.Translate;
                break;
            }
            case Action.Translate:
            {
                CurrentAction = Action.Rotate;
                break;
            }
            case Action.Rotate:
            {
                CurrentAction = Action.Scale;
                break;
            }
            case Action.Scale:
            {
                CurrentAction = Action.Spawn;
                break;
            }
            // case Action.Scale:
            // {
            //     CurrentAction = Action.Duplicate;
            //     break;
            // }
            // case Action.Duplicate:
            // {
            //     CurrentAction = Action.Spawn;
            //     break;
            // }
        }

        foreach (CSE165Interactable interactable in SelectedObjects)
        {
            interactable.SaveNewTransformData();
        }
    }

    private void SpawnAction()
    {
        GameObject PrefabToSpawn = SpawnObjects[SpawnObjectIndex];

        Vector3 spawnPos;
        Quaternion spawnRot;
        bool snap = false;
        bool didInteract = RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit);

        if (RayInteractor != null && didInteract)
        {
            spawnPos = hit.point;
            spawnRot = Quaternion.LookRotation(hit.normal);
            snap = true;
        }
        else
        {
            // Just spawn in front of the controller if nothing hit
            // TODO! Maybe do something else or just add invisible coliders to whole tent
            spawnPos = transform.position + transform.forward;
            spawnRot = transform.rotation;
        }

        LastSpawnedObject = Instantiate(PrefabToSpawn, spawnPos, spawnRot);

        if (snap)
        {
            LastSpawnedObject.transform.position += hit.normal * 0.25f;
        }

        ControllerPositionAtLastSpawn = transform.position;
        SpawnedObjectPositionAtLastSpawn = LastSpawnedObject.transform.position;
    }

    private void SpawnHold()
    {
        Vector3 DeltaPosition = ControllerPositionAtLastSpawn - transform.position;
        Quaternion CurrentControllerRotation = transform.rotation;

        if (LastSpawnedObject == null) return;

        // TODO! Make the speed factor a function of controller speed
        float speed = 5.0f;
        LastSpawnedObject.transform.position = SpawnedObjectPositionAtLastSpawn - DeltaPosition * speed;
        LastSpawnedObject.transform.rotation = CurrentControllerRotation;
        
        Rigidbody LastSpawnedObjectRigidBody = LastSpawnedObject.GetComponent<Rigidbody>();
        if (LastSpawnedObjectRigidBody)
        {
            LastSpawnedObjectRigidBody.velocity = Vector3.zero;
        }
    }
}