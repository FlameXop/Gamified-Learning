using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using FlamexStudios.Player;

namespace FlamexStudios.Exam
{
    public class BookInteractable : MonoBehaviour
    {
        [Header("1. Book Identity & Lock Level")]
        [Tooltip("Set 1 for Class 1, 2 for Class 2, up to 7")]
        public int classNumber = 1;
        public ExamData bookExamData;

        [Header("2. Camera & 3D Book Mesh")]
        [Tooltip("Your Cinemachine Camera positioned inside your Blur Box Volume")]
        public CinemachineCamera bookCinemachineCam;
        [Tooltip("Optional: The 3D Book FBX to lift when opening (Uncheck Static on it!)")]
        public Transform bookMeshTransform;
        public float cameraBlendTime = 0.8f;

        [Header("3. UI Prompt")]
        public TextMeshProUGUI promptText;
        public float interactDistance = 2.0f;

        private Vector3 originalBookPos;
        private Quaternion originalBookRot;
        private bool isPlayerNear = false;
        private bool isReading = false;
        private Collider triggerCollider;

        private void Start()
        {
            if (bookMeshTransform != null)
            {
                originalBookPos = bookMeshTransform.position;
                originalBookRot = bookMeshTransform.rotation;
            }

            triggerCollider = GetComponent<Collider>();

            // Keep book camera priority low until the player interacts
            if (bookCinemachineCam != null)
                bookCinemachineCam.Priority = 5;
        }

        private void Update()
        {
            if (isReading || bookExamData == null || PlayerManager.Instance == null) return;

            // Check if player is inside the trigger box OR within interactDistance
            Vector3 playerPos = PlayerManager.Instance.transform.position;
            bool inTrigger = triggerCollider != null && triggerCollider.bounds.Contains(playerPos + Vector3.up * 0.5f);
            bool inRange = Vector3.Distance(transform.position, playerPos) <= interactDistance;

            bool nearNow = inTrigger || inRange;

            if (nearNow && !isPlayerNear)
            {
                isPlayerNear = true;
                ShowPrompt(true);
            }
            else if (!nearNow && isPlayerNear)
            {
                isPlayerNear = false;
                ShowPrompt(false);
            }

            // Listen for Interact press (E / Gamepad Y)
            if (isPlayerNear)
            {
                bool interactPressed = (PlayerManager.Instance.Input != null && PlayerManager.Instance.Input.InteractTriggered) ||
                                       (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);

                if (interactPressed)
                {
                    if (PlayerManager.Instance.Input != null)
                        PlayerManager.Instance.Input.ConsumeInteract();

                    TryOpenBook();
                }
            }
        }

        private void TryOpenBook()
        {
            int unlockedLevel = CurriculumManager.Instance != null ? CurriculumManager.Instance.currentUnlockedTest : 1;

            // Lock future books until earlier tests are passed!
            if (classNumber > unlockedLevel)
            {
                if (CurriculumManager.Instance != null)
                    CurriculumManager.Instance.TriggerAccessDenied();

                ShowPrompt(true); // Refreshes prompt to show locked message
                return;
            }

            StartCoroutine(OpenBookSequence());
        }

        private void ShowPrompt(bool show)
        {
            if (promptText == null) return;

            if (show)
            {
                int unlockedLevel = CurriculumManager.Instance != null ? CurriculumManager.Instance.currentUnlockedTest : 1;
                PlayerInput pi = PlayerManager.Instance.GetComponent<PlayerInput>();
                string key = (pi != null && pi.currentControlScheme == "Gamepad") ? "Y" : "E";

                if (classNumber > unlockedLevel)
                {
                    promptText.text = $"[LOCKED - PASS TEST {classNumber - 1} FIRST] {bookExamData.examTitle}";
                }
                else
                {
                    promptText.text = $"Press [{key}] to Study: {bookExamData.examTitle}";
                }

                // Ensure Exam_Canvas and the prompt text object are active
                Transform t = promptText.transform;
                while (t != null)
                {
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t = t.parent;
                }
            }
            else
            {
                promptText.gameObject.SetActive(false);
            }
        }

        private IEnumerator OpenBookSequence()
        {
            isReading = true;
            ShowPrompt(false);

            // Lock movement & hide player body so it doesn't block the Book Camera
            if (PlayerManager.Instance.Movement != null)
                PlayerManager.Instance.Movement.enabled = false;
            PlayerManager.Instance.SetMeshVisibility(false);

            // Switch Cinemachine priority -> blends into your Book Camera & Blur Box Volume!
            if (bookCinemachineCam != null)
                bookCinemachineCam.Priority = 30;

            // Optional 3D book lift during camera blend
            if (bookMeshTransform != null && bookCinemachineCam != null)
            {
                Transform camT = bookCinemachineCam.transform;
                Vector3 targetPos = camT.position + (camT.forward * 0.45f) - (camT.up * 0.08f);
                Quaternion targetRot = Quaternion.LookRotation(-camT.forward, camT.up);

                float elapsed = 0f;
                while (elapsed < cameraBlendTime)
                {
                    elapsed += Time.deltaTime;
                    float smoothT = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / cameraBlendTime), 3f);
                    bookMeshTransform.position = Vector3.Lerp(originalBookPos, targetPos, smoothT);
                    bookMeshTransform.rotation = Quaternion.Slerp(originalBookRot, targetRot, smoothT);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(cameraBlendTime);
            }

            // Open the 2D Book UI on top of the blurred camera view
            LibraryBookUIManager.Instance.OpenBookUI(bookExamData, this);
        }

        // Called automatically when pressing Escape inside LibraryBookUIManager
        public void CloseAndPutDownBook()
        {
            if (bookMeshTransform != null)
            {
                bookMeshTransform.position = originalBookPos;
                bookMeshTransform.rotation = originalBookRot;
            }

            // Drop priority back to 5 -> Cinemachine smoothly blends back to Player FreeLook Cam
            if (bookCinemachineCam != null)
                bookCinemachineCam.Priority = 5;

            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.SetMeshVisibility(true);
                if (PlayerManager.Instance.Movement != null)
                    PlayerManager.Instance.Movement.enabled = true;
            }

            isReading = false;
        }
    }
}