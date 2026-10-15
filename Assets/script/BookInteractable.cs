using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using FlamexStudios.Player;

namespace FlamexStudios.Exam
{
    public class BookInteractable : MonoBehaviour
    {
        [Header("Book Content")]
        public ExamData bookExamData;

        [Header("3D Book Mesh to Animate")]
        public Transform bookMeshTransform;

        [Header("UI Prompt (Only Prompt Text is required!)")]
        public GameObject interactPromptUI;
        public TextMeshProUGUI promptText;

        private Vector3 originalWorldPos;
        private Quaternion originalWorldRot;
        private bool isPlayerNear = false;
        private bool isReading = false;

        private void Start()
        {
            if (bookMeshTransform == null) bookMeshTransform = transform;
            originalWorldPos = bookMeshTransform.position;
            originalWorldRot = bookMeshTransform.rotation;

            // Auto-link Interact Prompt UI if only Prompt Text was assigned in the Inspector!
            if (interactPromptUI == null && promptText != null)
            {
                interactPromptUI = promptText.gameObject;
            }
        }

        private bool IsPlayerCollider(Collider other)
        {
            return other.CompareTag("Player") ||
                   other.GetComponent<PlayerManager>() != null ||
                   other.GetComponentInParent<PlayerManager>() != null;
        }

        private void ForceEnableWithParents(GameObject target)
        {
            if (target == null) return;
            Transform t = target.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayerCollider(other) && !isReading && bookExamData != null)
            {
                isPlayerNear = true;
                UpdatePrompt();

                // Automatically wakes up Exam_Canvas and the prompt text!
                if (interactPromptUI != null) ForceEnableWithParents(interactPromptUI);
                Debug.Log($"[BookInteractable] Player entered book trigger: {bookExamData.examTitle}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                isPlayerNear = false;
                if (interactPromptUI != null) interactPromptUI.SetActive(false);
            }
        }

        private void UpdatePrompt()
        {
            if (promptText == null || bookExamData == null) return;
            PlayerInput pi = PlayerManager.Instance != null ? PlayerManager.Instance.GetComponent<PlayerInput>() : null;
            string key = (pi != null && pi.currentControlScheme == "Gamepad") ? "Y" : "E";
            promptText.text = $"Press [{key}] to Read: {bookExamData.examTitle}";
        }

        private void Update()
        {
            if (!isPlayerNear || isReading) return;
            if (PlayerManager.Instance == null || PlayerManager.Instance.Input == null) return;

            if (PlayerManager.Instance.Input.InteractTriggered)
            {
                PlayerManager.Instance.Input.ConsumeInteract();
                StartCoroutine(PickUpBookSequence());
            }
        }

        private IEnumerator PickUpBookSequence()
        {
            isReading = true;
            if (interactPromptUI != null) interactPromptUI.SetActive(false);

            PlayerManager.Instance.Movement.enabled = false;

            if (Camera.main != null && bookMeshTransform != null)
            {
                Transform camTransform = Camera.main.transform;
                Vector3 targetPos = camTransform.position + (camTransform.forward * 0.45f) - (camTransform.up * 0.1f);
                Quaternion targetRot = Quaternion.LookRotation(-camTransform.forward, camTransform.up);

                float duration = 0.4f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = 1f - Mathf.Pow(1f - (elapsed / duration), 3f);

                    bookMeshTransform.position = Vector3.Lerp(originalWorldPos, targetPos, t);
                    bookMeshTransform.rotation = Quaternion.Slerp(originalWorldRot, targetRot, t);
                    yield return null;
                }
            }

            if (bookMeshTransform != null && bookMeshTransform != transform)
                bookMeshTransform.gameObject.SetActive(false);

            if (LibraryBookUIManager.Instance != null)
            {
                LibraryBookUIManager.Instance.OpenBookUI(bookExamData, this);
            }
            else
            {
                Debug.LogError("[BookInteractable] LibraryBookUIManager.Instance is missing in the scene!");
            }
        }

        public void PutDownBook()
        {
            if (bookMeshTransform != null)
            {
                bookMeshTransform.position = originalWorldPos;
                bookMeshTransform.rotation = originalWorldRot;
                if (bookMeshTransform != transform)
                    bookMeshTransform.gameObject.SetActive(true);
            }

            PlayerManager.Instance.Movement.enabled = true;
            isReading = false;
        }
    }
}