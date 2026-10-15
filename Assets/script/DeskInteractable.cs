using UnityEngine;
using Unity.Cinemachine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

namespace FlamexStudios.Exam
{
    public class DeskInteractable : MonoBehaviour
    {
        [Header("Exam Assignment & Lock ID")]
        [Tooltip("Set 1 for Class 1, 2 for Class 2, up to 7")]
        public int testNumber = 1;
        public ExamData deskExam;
        public bool isFrontRow = false;

        [Header("Cameras")]
        public CinemachineCamera playerFreeLookCam;
        public CinemachineCamera deskFirstPersonCam;

        [Header("UI Prompts")]
        public GameObject interactPromptUI;
        public TextMeshProUGUI promptText;

        private bool isPlayerNear = false;
        private bool isTakingTest = false;
        private bool isCompleted = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isTakingTest)
            {
                isPlayerNear = true;
                UpdatePromptText();
                if (interactPromptUI != null) interactPromptUI.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerNear = false;
                if (interactPromptUI != null) interactPromptUI.SetActive(false);
            }
        }

        private void UpdatePromptText()
        {
            if (promptText == null || deskExam == null) return;

            int unlockedLevel = CurriculumManager.Instance != null ? CurriculumManager.Instance.currentUnlockedTest : 1;

            if (isCompleted || testNumber < unlockedLevel)
            {
                promptText.text = $"[COMPLETED] {deskExam.examTitle}";
                return;
            }

            if (testNumber > unlockedLevel)
            {
                promptText.text = $"[LOCKED - COMPLETE CLASS {unlockedLevel} FIRST] {deskExam.examTitle}";
                return;
            }

            PlayerInput playerInput = Player.PlayerManager.Instance.GetComponent<PlayerInput>();
            string keyName = (playerInput != null && playerInput.currentControlScheme == "Gamepad") ? "Y" : "E";
            promptText.text = $"Press [{keyName}] to start {deskExam.examTitle}";
        }

        private void Update()
        {
            if (isPlayerNear && !isTakingTest && Player.PlayerManager.Instance.Input.InteractTriggered)
            {
                Player.PlayerManager.Instance.Input.ConsumeInteract();
                TryStartExam();
            }
        }

        private void TryStartExam()
        {
            int unlockedLevel = CurriculumManager.Instance != null ? CurriculumManager.Instance.currentUnlockedTest : 1;

            // Block player if test is locked or already beaten!
            if (isCompleted || testNumber != unlockedLevel)
            {
                if (CurriculumManager.Instance != null)
                    CurriculumManager.Instance.TriggerAccessDenied();
                return;
            }

            StartExamSequence();
        }

        private void StartExamSequence()
        {
            isTakingTest = true;
            if (interactPromptUI != null) interactPromptUI.SetActive(false);

            Player.PlayerManager.Instance.Movement.enabled = false;
            Player.PlayerManager.Instance.SetMeshVisibility(false);
            Player.PlayerManager.Instance.Animator.SetTrigger("SitDown");

            deskFirstPersonCam.Priority = 20;
            playerFreeLookCam.Priority = 10;

            ExamUIManager.Instance.SetExamState(ExamState.Transitioning);
            StartCoroutine(WaitForCameraThenStart());
        }

        private IEnumerator WaitForCameraThenStart()
        {
            yield return new WaitForSeconds(1.5f);
            ExamUIManager.Instance.TriggerWelcomeScreen(deskExam, this);
        }

        // Called by ExamUIManager when the player passes the test!
        public void CompleteAndStandUp()
        {
            isCompleted = true;
            isTakingTest = false;

            deskFirstPersonCam.Priority = 10;
            playerFreeLookCam.Priority = 20;

            Player.PlayerManager.Instance.SetMeshVisibility(true);
            Player.PlayerManager.Instance.Movement.enabled = true;

            if (CurriculumManager.Instance != null)
                CurriculumManager.Instance.OnExamPassed();
        }
    }
}