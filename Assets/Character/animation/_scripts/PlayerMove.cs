using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMove : MonoBehaviour

{
    public InputAction moveAction;
    public bool jumpTrigger;
    public bool moveTrigger;
    public Vector2 moveValue;
    public float speed =6;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Player/Move");
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }
    public void Move()
    {
        moveValue = moveAction.ReadValue<Vector2>();
        Vector2 delta = moveValue * Time.deltaTime * speed;
        transform.position += new Vector3(delta.x, 0, delta.y);
        moveTrigger = moveAction.IsPressed();
        Debug.Log(moveTrigger);

    }
}
