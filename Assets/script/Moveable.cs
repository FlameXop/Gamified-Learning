using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Moveable : PlayerMove
{
    public Transform camTransform;
    public Transform player;
    public Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Player/Move");
        camTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 cameraForward = camTransform.forward;
        Vector3 cameraRight = camTransform.right;
        Vector3 cameraUp = camTransform.up;
        cameraUp.x = 0f;
        cameraUp.z = 0f;
        cameraUp.Normalize();
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraRight.Normalize();
        cameraForward.Normalize();
        moveValue = moveAction.ReadValue<Vector2>();
        transform.forward = cameraForward;
        if (escape())
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        
        Vector3 total = (moveValue.x * cameraRight)+ (moveValue.y * cameraForward);
        transform.position  += total * speed * Time.deltaTime;
        animator.SetFloat("Velocity", total.magnitude);
        if (jump())
        {
            // Apply an instant upward force (replace 5f with your desired jump power)
            GetComponent<Rigidbody>().AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }
    }
    
}
