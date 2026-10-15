using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

namespace FlamexStudios.Exam
{
    public class LibraryBookUIManager : MonoBehaviour
    {
        public static LibraryBookUIManager Instance { get; private set; }

        [Header("All 7 Curriculum Books (Optional Reference List)")]
        public List<BookInteractable> allLibraryBooks = new List<BookInteractable>();

        [Header("Book UI Panels")]
        public GameObject bookUIPanel;            // Drag BookPanel here
        public RectTransform bookSpreadRect;      // Drag Book_Spread_Container here
        public CanvasGroup bookCanvasGroup;       // Optional CanvasGroup on BookPanel

        [Header("Left & Right Page Text")]
        public TextMeshProUGUI bookTitleText;     // Drag Book_Title_Text here
        public TextMeshProUGUI leftPageText;      // Drag Left_Page_Text here
        public TextMeshProUGUI rightPageText;     // Drag Right_Page_Text here
        public TextMeshProUGUI pageNumberText;    // Drag Page_Number_Text here
        public int notesPerPage = 5;

        [Header("Audio")]
        public AudioSource bookAudioSource;
        public AudioClip bookOpenSFX;
        public AudioClip pageFlipSFX;
        public AudioClip bookCloseSFX;

        public bool IsBookOpen { get; private set; }

        private ExamData currentBookData;
        private BookInteractable currentActiveBook;
        private int currentSpreadIndex = 0;
        private int totalSpreads = 1;
        private bool isTransitioning = false;

        private const string GLITCH_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%&*<>[]/_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (bookUIPanel != null) bookUIPanel.SetActive(false);
        }

        private void Update()
        {
            if (!IsBookOpen) return;

            // Standard UX: Press ESC (or Controller East Button) to close the book and return
            bool closePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                                (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

            if (closePressed)
            {
                CloseBook();
            }

            // Optional UX: Left/Right Arrow keys or A/D also turn pages alongside the UI buttons!
            if (!isTransitioning && Keyboard.current != null)
            {
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                    NextPage();
                else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                    PreviousPage();
            }
        }

        public void OpenBookUI(ExamData bookData, BookInteractable bookTrigger)
        {
            currentBookData = bookData;
            currentActiveBook = bookTrigger;
            currentSpreadIndex = 0;
            IsBookOpen = true;

            int notesPerSpread = notesPerPage * 2;
            totalSpreads = Mathf.Max(1, Mathf.CeilToInt((float)currentBookData.questions.Count / notesPerSpread));

            if (bookTitleText != null)
                bookTitleText.text = $"{currentBookData.examTitle}   [Press ESC to Close]";

            // Ensure Exam_Canvas and BookPanel are active
            if (bookUIPanel != null)
            {
                Transform t = bookUIPanel.transform;
                while (t != null)
                {
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t = t.parent;
                }
                bookUIPanel.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (bookAudioSource != null && bookOpenSFX != null)
                bookAudioSource.PlayOneShot(bookOpenSFX);

            StopAllCoroutines();
            StartCoroutine(OpenBookPopRoutine());
        }

        private IEnumerator OpenBookPopRoutine()
        {
            isTransitioning = true;
            float duration = 0.22f;
            float elapsed = 0f;

            if (leftPageText != null) leftPageText.text = "";
            if (rightPageText != null) rightPageText.text = "";

            if (bookCanvasGroup != null) bookCanvasGroup.alpha = 0f;
            if (bookSpreadRect != null) bookSpreadRect.localScale = new Vector3(0.85f, 0.85f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float easeOut = 1f - Mathf.Pow(1f - (elapsed / duration), 3f);

                if (bookCanvasGroup != null) bookCanvasGroup.alpha = easeOut;
                if (bookSpreadRect != null)
                    bookSpreadRect.localScale = Vector3.Lerp(new Vector3(0.85f, 0.85f, 1f), Vector3.one, easeOut);

                yield return null;
            }

            if (bookCanvasGroup != null) bookCanvasGroup.alpha = 1f;
            if (bookSpreadRect != null) bookSpreadRect.localScale = Vector3.one;

            yield return StartCoroutine(GlitchPopSpreadText());
            isTransitioning = false;
        }

        public void NextPage()
        {
            if (isTransitioning || currentSpreadIndex >= totalSpreads - 1) return;
            currentSpreadIndex++;
            if (bookAudioSource != null && pageFlipSFX != null)
                bookAudioSource.PlayOneShot(pageFlipSFX);

            StopAllCoroutines();
            StartCoroutine(GlitchPopSpreadText());
        }

        public void PreviousPage()
        {
            if (isTransitioning || currentSpreadIndex <= 0) return;
            currentSpreadIndex--;
            if (bookAudioSource != null && pageFlipSFX != null)
                bookAudioSource.PlayOneShot(pageFlipSFX);

            StopAllCoroutines();
            StartCoroutine(GlitchPopSpreadText());
        }

        private IEnumerator GlitchPopSpreadText()
        {
            isTransitioning = true;

            int startIndex = currentSpreadIndex * (notesPerPage * 2);
            string targetLeft = BuildPageString(startIndex, notesPerPage);
            string targetRight = BuildPageString(startIndex + notesPerPage, notesPerPage);

            if (pageNumberText != null)
            {
                int leftNum = (currentSpreadIndex * 2) + 1;
                int rightNum = leftNum + 1;
                pageNumberText.text = $"Page {leftNum} - {rightNum} / {totalSpreads * 2}";
            }

            RectTransform leftRect = leftPageText != null ? leftPageText.rectTransform : null;
            RectTransform rightRect = rightPageText != null ? rightPageText.rectTransform : null;

            float duration = 0.25f;
            float elapsed = 0f;

            char[] leftBuffer = new char[targetLeft.Length];
            char[] rightBuffer = new char[targetRight.Length];

            while (elapsed < duration)
            {
                elapsed += 0.02f;
                float progress = Mathf.Clamp01(elapsed / duration);
                float popScale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.05f;

                if (leftRect != null) leftRect.localScale = new Vector3(popScale, popScale, 1f);
                if (rightRect != null) rightRect.localScale = new Vector3(popScale, popScale, 1f);

                if (leftPageText != null) leftPageText.text = ScrambleText(targetLeft, leftBuffer, progress);
                if (rightPageText != null) rightPageText.text = ScrambleText(targetRight, rightBuffer, progress);

                yield return new WaitForSeconds(0.02f);
            }

            if (leftRect != null) leftRect.localScale = Vector3.one;
            if (rightRect != null) rightRect.localScale = Vector3.one;
            if (leftPageText != null) leftPageText.text = targetLeft;
            if (rightPageText != null) rightPageText.text = targetRight;

            isTransitioning = false;
        }

        private string ScrambleText(string finalText, char[] buffer, float progress)
        {
            int revealedCount = Mathf.FloorToInt(progress * finalText.Length);
            for (int i = 0; i < finalText.Length; i++)
            {
                char c = finalText[i];
                if (c == ' ' || c == '\n' || c == '\r' || c == '•')
                    buffer[i] = c;
                else if (i < revealedCount)
                    buffer[i] = c;
                else
                    buffer[i] = GLITCH_CHARS[Random.Range(0, GLITCH_CHARS.Length)];
            }
            return new string(buffer);
        }

        private string BuildPageString(int startIdx, int count)
        {
            if (currentBookData == null || currentBookData.questions == null) return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<Question> list = currentBookData.questions;

            for (int i = startIdx; i < startIdx + count && i < list.Count; i++)
            {
                // Prints bookStudyNote if filled, otherwise prints the Question + Answer automatically
                string entry = !string.IsNullOrEmpty(list[i].bookStudyNote)
                    ? list[i].bookStudyNote
                    : $"{list[i].questionText} -> {list[i].correctTextAnswer}";

                sb.AppendLine($"• {entry}\n");
            }
            return sb.ToString();
        }

        public void CloseBook()
        {
            if (!IsBookOpen) return;
            IsBookOpen = false;
            StopAllCoroutines();

            if (bookAudioSource != null && bookCloseSFX != null)
                bookAudioSource.PlayOneShot(bookCloseSFX);

            if (bookUIPanel != null) bookUIPanel.SetActive(false);

            if (currentActiveBook != null)
            {
                currentActiveBook.CloseAndPutDownBook();
            }

            if (ExamUIManager.Instance == null || !ExamUIManager.Instance.IsChitOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}