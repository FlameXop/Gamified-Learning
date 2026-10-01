using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMove : MonoBehaviour

{
    public InputAction moveAction;
    public InputAction Escape;
    public InputAction Jump;
    public bool jumpTrigger;
    // Movement TRIGGERED
    public bool moveTrigger;
//escape pressed
    public bool escapeTrigger;
    public Vector2 moveValue;
    public float speed =0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Player/Move");
        Escape = InputSystem.actions.FindAction("Player/Escape");
        Jump = InputSystem.actions.FindAction("Player/Jump");

    }

    // Update is called once per frame
    void Update()
    {
        Move();
        jump();
    }
    public void Move()
    {
        moveValue = moveAction.ReadValue<Vector2>();
        Vector2 delta = moveValue * Time.deltaTime * speed;
        transform.position += new Vector3(delta.x, 0, delta.y);
        moveTrigger = moveAction.IsPressed();
        Debug.Log(moveTrigger);

    }
    public bool escape()
    {
        escapeTrigger = Escape.IsPressed();
        return escapeTrigger;
    }
    public bool jump()
    {
        // WasPressedThisFrame() ensures they only jump once per button press,
        // rather than flying while the button is held down.
        return Jump.WasPressedThisFrame();
    }
}
