using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ProjectOneLeftController : MonoBehaviour
{
    public XRRayInteractor RayInteractor;

    public GameObject RightControllerObject;
    private ProjectOneRightController RightControllerScript;

    public ActionBasedController LeftControllerInput;
    public GameObject XRRig;
    public GameObject Camera;

    private bool CycleWasHeld = false;

    public float MoveSpeed = 10.0f;
    private bool ActionWasHeld = false;

    // Handle inputs
    private void Update()
    {
        if (LeftControllerInput.selectAction.action.IsPressed())
        {
            if (!CycleWasHeld)
            {
                CycleWasHeld = true;
                OnCycle();
            }
        } else
        {
            CycleWasHeld = false;
        }

        if (LeftControllerInput.activateAction.action.IsPressed())
        {
            if (!ActionWasHeld)
            {
                ActionWasHeld = true;
                MoveAction();
            }
            else
            {
                MoveHold();
            }
        }
        else
        {
            ActionWasHeld = false;
        }
    }

    private void Start()
    {
        RightControllerScript = RightControllerObject.GetComponent<ProjectOneRightController>();
    }

    private void OnCycle()
    {
        RightControllerScript.CycleAction();
    }

    private void MoveAction()
    {
        // Only y-axis rotation
        Vector3 asEuler = transform.rotation.eulerAngles;
        asEuler.x = 0.0f;
        asEuler.z = 0.0f;
        XRRig.transform.rotation = Quaternion.Euler(asEuler);
    }

    private void MoveHold()
    {
        Vector3 direction = transform.forward;
        direction.y = 0;
        XRRig.transform.position += direction * Time.deltaTime;
    }
}
