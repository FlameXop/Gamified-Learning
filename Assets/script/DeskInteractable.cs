using UnityEngine;
using Unity.Cinemachine;
using TMPro; // Needed for text
using UnityEngine.InputSystem; // Needed to check gamepad vs keyboard

namespace FlamexStudios.Exam
{
    public class DeskInteractable : MonoBehaviour
    {
        [Header("Exam Assignment")]
        public ExamData deskExam;
        public bool isFrontRow = false;

        [Header("Cameras")]
        public CinemachineCamera playerFreeLookCam;
        public CinemachineCamera deskFirstPersonCam;

        [Header("UI Prompts")]
        public GameObject interactPromptUI;
        public TextMeshProUGUI promptText; // ADD THIS: To change the actual letters

        private bool isPlayerNear = false;
        private bool isTakingTest = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNear = true;
                UpdatePromptText(); // Check device before showing
                interactPromptUI.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNear = false;
                interactPromptUI.SetActive(false);
            }
        }

        private void UpdatePromptText()
        {
            if (promptText == null || deskExam == null) return;

            // Get the PlayerInput component to see what device is active
            PlayerInput playerInput = Player.PlayerManager.Instance.GetComponent<PlayerInput>();

            // "Gamepad" and "KeyboardMouse" are the default scheme names in Unity's Input System.
            // Check your Player Input Actions asset to confirm your exact scheme names!
            if (playerInput.currentControlScheme == "Gamepad")
            {
                promptText.text = "Press [Y] to start " + deskExam.examTitle;
            }
            else
            {
                promptText.text = "Press [E] to start " + deskExam.examTitle;
            }
        }

        private void Update()
        {
            // Update to use the New Input System boolean we made earlier
            if (isPlayerNear && !isTakingTest && Player.PlayerManager.Instance.Input.InteractTriggered)
            {
                Player.PlayerManager.Instance.Input.ConsumeInteract();
                StartExamSequence();
            }
        }

        private void StartExamSequence()
        {
            isTakingTest = true;
            interactPromptUI.SetActive(false);

            Player.PlayerManager.Instance.Movement.enabled = false;
            Player.PlayerManager.Instance.Animator.SetTrigger("SitDown");
            // This is the line that actually pulls the trigger to hide the mesh!
            Player.PlayerManager.Instance.SetMeshVisibility(false);

            deskFirstPersonCam.Priority = 20;
            playerFreeLookCam.Priority = 10;

            // Start a Coroutine to wait for the camera blend to finish!
            StartCoroutine(WaitForCameraThenStart());
        }

        private System.Collections.IEnumerator WaitForCameraThenStart()
        {
            // Cinemachine default blend is usually 2 seconds. We wait 1.5s for a cinematic feel.
            yield return new WaitForSeconds(1.5f);

            // NOW we trigger the Welcome Screen!
            ExamUIManager.Instance.TriggerWelcomeScreen(deskExam, this);
        }
    }
}