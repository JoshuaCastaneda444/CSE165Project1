using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSE165Interactable : MonoBehaviour
{
    // Selection Logic
    private Material OriginalMaterial;
    public Material HighlightMaterial;
    public Material SelectedMaterial;
    public enum InteractionState { Default, Highlighted, Selected };
    public InteractionState ColorState = InteractionState.Default;
    private Renderer MaterialRenderer;
    private bool Selected = false;
    private bool Highlighted = false;

    // Manipulation Variables
    private Vector3 OriginalPosition;
    private Quaternion OriginalRotation;
    private Vector3 OrignalScale;
    private Vector3 ControllerOriginalPosition;
    public GameObject DuplicatePrefab;

    public void Start()
    {
        MaterialRenderer = GetComponent<Renderer>();
        OriginalMaterial = MaterialRenderer.material;
    }

    private void SetMaterial(Material material)
    {
        MaterialRenderer.material = material;
    }

    public void Highlight()
    {
        SetMaterial(HighlightMaterial);
        ColorState = InteractionState.Highlighted;
        Highlighted = true;
    }

    public void Unhighlight()
    {
        if (Selected)
        {
            SetMaterial(SelectedMaterial);
            ColorState = InteractionState.Selected;
        }
        else
        {
            SetMaterial(OriginalMaterial);
            ColorState = InteractionState.Default;
        }
        Highlighted = false;
    }

    public void Select()
    {
        // Selected via area select
        if (ColorState == InteractionState.Default)
        {
            SetMaterial(SelectedMaterial);
            ColorState = InteractionState.Selected;
        }
        Selected = true;
    }

    public void Deselect()
    {
        // Via right pointer
        if (Highlighted)
        {
            /* no-op */
        }
        // Via area select
        else
        {
            SetMaterial(OriginalMaterial);
            ColorState = InteractionState.Default;
        }
        Selected = false;
    }

    public void SaveNewTransformData()
    {
        OriginalPosition = transform.position;
        OriginalRotation = transform.rotation;
        OrignalScale = transform.localScale;
    }

    private void KeepStatic()
    {
        Rigidbody RigidBody = GetComponent<Rigidbody>();
        if (RigidBody)
        {
            RigidBody.velocity = Vector3.zero;
        }
    }

    public void TranslateAction(Vector3 ControllerPosition)
    {
        SaveNewTransformData();
        ControllerOriginalPosition = ControllerPosition;
    }

    public void TranslateHold(Vector3 ControllerPosition)
    {
        Vector3 DeltaPosition = ControllerOriginalPosition - ControllerPosition;

        // TODO! Make the speed factor a function of controller speed
        float speed = 5.0f;
        transform.position = OriginalPosition - DeltaPosition * speed;
        transform.rotation = OriginalRotation;

        KeepStatic();
    }

    public void RotateAction()
    {
        SaveNewTransformData();
    }

    public void RotateHold(Quaternion ControllerRotation)
    {
        transform.rotation = ControllerRotation;
        transform.position = OriginalPosition;
        KeepStatic();
    }

    public void ScaleAction(Vector3 ControllerPosition)
    {
        SaveNewTransformData();
        ControllerOriginalPosition = ControllerPosition;
    }

    public void ScaleHold(Vector3 ControllerPosition)
    {
        transform.position = OriginalPosition;
        transform.rotation = OriginalRotation;

        transform.localScale = OrignalScale * Mathf.Clamp(ControllerPosition.y - ControllerOriginalPosition.y + 1.0f, 0.01f, Mathf.Infinity);
        KeepStatic();
    }

    // Duplicate incomplete...
    public void DuplicateAction(Vector3 ControllerPosition)
    {
        Instantiate(DuplicatePrefab, new Vector3(transform.position.x, transform.position.y + 2.0f, transform.position.z), transform.rotation);
        ControllerOriginalPosition = ControllerPosition;
        SaveNewTransformData();
    }

    public void DuplicateHold()
    {
        Vector3 DeltaPosition = ControllerOriginalPosition - transform.position;
        Quaternion CurrentControllerRotation = transform.rotation;

        // TODO! Make the speed factor a function of controller speed
        float speed = 5.0f;
        transform.position = OriginalPosition - DeltaPosition * speed;
        transform.rotation = CurrentControllerRotation;
        
        Rigidbody LastSpawnedObjectRigidBody = GetComponent<Rigidbody>();
        if (LastSpawnedObjectRigidBody)
        {
            LastSpawnedObjectRigidBody.velocity = Vector3.zero;
        }
    }
}
