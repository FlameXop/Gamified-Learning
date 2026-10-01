using UnityEngine;
using UnityEngine.InputSystem;

namespace FlamexStudios.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public bool JumpTriggered { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool InteractTriggered { get; private set; } // Ready for future expansion
      
        public bool ChitToggleTriggered { get; private set; }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started) JumpTriggered = true;
            if (context.canceled) JumpTriggered = false;
        }
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started) InteractTriggered = true;
            if (context.canceled) InteractTriggered = false;
        }

        public void OnToggleChit(InputAction.CallbackContext context)
        {
            if (context.started) ChitToggleTriggered = true;
            if (context.canceled) ChitToggleTriggered = false;
        }

        public void ConsumeInteract() => InteractTriggered = false;
        public void ConsumeChitToggle() => ChitToggleTriggered = false;

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started) IsSprinting = true;
            if (context.canceled) IsSprinting = false;
        }

        public void ConsumeJump() => JumpTriggered = false;
    }

}