using UnityEngine;

namespace FlamexStudios.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Stats")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 7f;
        public float rotationSmoothTime = 0.1f;

        [Header("Jumping & Gravity")]
        public float jumpHeight = 1.2f;
        public float gravity = -15f;
        public Transform groundCheck;
        public float groundDistance = 0.4f;
        public LayerMask groundMask;

        private CharacterController controller;
        private Transform mainCamera;
        private PlayerManager player;

        private Vector3 velocity;
        private bool isGrounded;
        private float turnSmoothVelocity;
        [Header("Vaulting Detection")]
        public float vaultCheckDistance = 1f;    // How far ahead to check for a wall
        public float vaultCheckHeight = 0.5f;    // Height of the laser (chest/knee level)
        public LayerMask vaultLayer;             // Create a new layer called "Vaultable" for crates/walls

        // Animation Hashes (Highly optimized for AAA)
        private readonly int animSpeed = Animator.StringToHash("Speed");
        private readonly int animMotionX = Animator.StringToHash("MotionX");
        private readonly int animMotionZ = Animator.StringToHash("MotionZ");
        private readonly int animGrounded = Animator.StringToHash("IsGrounded");
        private readonly int animJump = Animator.StringToHash("Jump");
        
        private readonly int animVault = Animator.StringToHash("Vault");

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            player = PlayerManager.Instance;
            mainCamera = Camera.main.transform;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            HandleGravityAndJump();
            HandleMovement();

        }
  

        private void HandleMovement()
        {
            Vector2 input = player.Input.MoveInput;
            Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

            float currentSpeed = player.Input.IsSprinting ? sprintSpeed : walkSpeed;
            float targetSpeed = direction.magnitude >= 0.1f ? currentSpeed : 0f;

            if (direction.magnitude >= 0.1f)
            {
                // Camera-relative rotation
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                // Move in the rotated direction
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                controller.Move(moveDir.normalized * targetSpeed * Time.deltaTime);
            }

            if (isGrounded)
            {
                UpdateAnimator(input, targetSpeed);
            }
        
        }

        private void HandleGravityAndJump()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            if (player.Input.JumpTriggered && isGrounded)
            {
                // 1. Define where the raycast starts (chest/knee height)
                Vector3 rayStart = transform.position + (Vector3.up * vaultCheckHeight);

                // 2. Shoot raycast forward to detect physical objects
                if (Physics.Raycast(rayStart, transform.forward, out RaycastHit hit, vaultCheckDistance, vaultLayer))
                {
                    // Obstacle detected! Trigger Vault.
                    player.Animator.SetTrigger(animVault);
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    // Note: For AAA, you'd usually disable character controller here and let Root Motion pull you over
                }
                else
                {
                    // No obstacle. Trigger Normal Jump.
                    player.Animator.SetTrigger(animJump);
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }

                player.Input.ConsumeJump();
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            player.Animator.SetBool(animGrounded, isGrounded);
        }

        // Add this so you can see the Vault Raycast in the Editor
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }

            // Draw the blue Vault detection laser
            Gizmos.color = Color.blue;
            Vector3 rayStart = transform.position + (Vector3.up * vaultCheckHeight);
            Gizmos.DrawRay(rayStart, transform.forward * vaultCheckDistance);
        }

        private void UpdateAnimator(Vector2 input, float currentSpeed)
        {
            // If walkSpeed is 3 and sprintSpeed is 7, multiplier becomes 1 for walking and ~2.33 for sprinting
            float speedMultiplier = currentSpeed / walkSpeed;

            player.Animator.SetFloat(animSpeed, currentSpeed, 0.1f, Time.deltaTime);

            // Pushes the Blend Tree coordinates further out when sprinting
            player.Animator.SetFloat(animMotionX, input.x * speedMultiplier, 0.1f, Time.deltaTime);
            player.Animator.SetFloat(animMotionZ, input.y * speedMultiplier, 0.1f, Time.deltaTime);
        }
    }
}