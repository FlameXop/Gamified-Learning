using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Moveable : PlayerMove
{
    public Transform camTransform;
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
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraRight.Normalize();
        cameraForward.Normalize();
        moveValue = moveAction.ReadValue<Vector2>();
        transform.forward = cameraForward;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        
        Vector3 total = (moveValue.x * cameraRight)+ (moveValue.y * cameraForward);
        transform.position  += total * speed * Time.deltaTime;
        animator.SetFloat("Velocity", total.magnitude);
    }
    
}
