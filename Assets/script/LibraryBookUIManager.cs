using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using FlamexStudios.Player;

namespace FlamexStudios.Exam
{
    public class LibraryBookUIManager : MonoBehaviour
    {
        public static LibraryBookUIManager Instance { get; private set; }

        [Header("Book UI Panels")]
        public GameObject bookUIPanel;            // Drag BookPanel here
        public RectTransform bookSpreadRect;      // Drag BookPanel or Book_Spread_Container here
        public CanvasGroup bookCanvasGroup;       // Optional: CanvasGroup on BookPanel

        [Header("Left & Right Page Text")]
        public TextMeshProUGUI bookTitleText;     // Drag Book_Title_Text here
        public TextMeshProUGUI leftPageText;      // Drag Left_Page_Text here
        public TextMeshProUGUI rightPageText;     // Drag Right_Page_Text here
        public TextMeshProUGUI pageNumberText;    // Drag Page_Number_Text here
        [Tooltip("How many study notes/words appear on a single page")]
        public int notesPerPage = 5;

        [Header("Audio")]
        public AudioSource bookAudioSource;
        public AudioClip bookOpenSFX;
        public AudioClip pageFlipSFX;

        public bool IsBookOpen { get; private set; }

        private ExamData currentBookData;
        private BookInteractable current3DBook;
        private int currentSpreadIndex = 0; // 0 = Pages 1-2, 1 = Pages 3-4, etc.
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

            // Allow closing the book with Escape (Keyboard) or East Button / B / Circle (Gamepad)
            if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
            {
                CloseBook();
            }
        }

        // ==========================================
        // 1. OPEN BOOK & INITIAL GLITCH POP
        // ==========================================
        public void OpenBookUI(ExamData bookData, BookInteractable bookObject)
        {
            currentBookData = bookData;
            current3DBook = bookObject;
            currentSpreadIndex = 0;
            IsBookOpen = true;

            int notesPerSpread = notesPerPage * 2;
            totalSpreads = Mathf.Max(1, Mathf.CeilToInt((float)currentBookData.questions.Count / notesPerSpread));

            if (bookTitleText != null)
                bookTitleText.text = currentBookData.examTitle;

            // Ensure BookPanel and Exam_Canvas are active
            if (bookUIPanel != null)
            {
                Transform t = bookUIPanel.transform;
                while (t != null)
                {
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t = t.parent;
                }
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
            float duration = 0.25f;
            float elapsed = 0f;

            if (leftPageText != null) leftPageText.text = "";
            if (rightPageText != null) rightPageText.text = "";

            if (bookCanvasGroup != null) bookCanvasGroup.alpha = 0f;
            if (bookSpreadRect != null) bookSpreadRect.localScale = new Vector3(0.8f, 0.8f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                if (bookCanvasGroup != null) bookCanvasGroup.alpha = easeOut;
                if (bookSpreadRect != null)
                    bookSpreadRect.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, easeOut);

                yield return null;
            }

            if (bookCanvasGroup != null) bookCanvasGroup.alpha = 1f;
            if (bookSpreadRect != null) bookSpreadRect.localScale = Vector3.one;

            // Glitch-pop the first two pages of text in!
            yield return StartCoroutine(GlitchPopSpreadText());
            isTransitioning = false;
        }

        // ==========================================
        // 2. NEXT / PREVIOUS PAGE (GLITCH POP TEXT)
        // ==========================================
        public void NextPage()
        {
            if (isTransitioning || currentSpreadIndex >= totalSpreads - 1) return;
            currentSpreadIndex++;
            ChangePageSpread();
        }

        public void PreviousPage()
        {
            if (isTransitioning || currentSpreadIndex <= 0) return;
            currentSpreadIndex--;
            ChangePageSpread();
        }

        private void ChangePageSpread()
        {
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
                int leftPageNum = (currentSpreadIndex * 2) + 1;
                int rightPageNum = leftPageNum + 1;
                pageNumberText.text = $"Page {leftPageNum} - {rightPageNum} / {totalSpreads * 2}";
            }

            RectTransform leftRect = leftPageText != null ? leftPageText.rectTransform : null;
            RectTransform rightRect = rightPageText != null ? rightPageText.rectTransform : null;

            float duration = 0.28f;
            float elapsed = 0f;

            char[] leftBuffer = new char[targetLeft.Length];
            char[] rightBuffer = new char[targetRight.Length];

            while (elapsed < duration)
            {
                elapsed += 0.02f;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Pop scale + slight glitch jitter on the text boxes
                float popScale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.06f;
                float jitterX = (progress < 0.7f) ? Random.Range(-4f, 4f) : 0f;

                if (leftRect != null) leftRect.localScale = new Vector3(popScale, popScale, 1f);
                if (rightRect != null) rightRect.localScale = new Vector3(popScale, popScale, 1f);

                if (bookSpreadRect != null)
                    bookSpreadRect.localRotation = Quaternion.Euler(0f, 0f, jitterX * 0.15f);

                // Scramble and decode Left & Right page text simultaneously
                if (leftPageText != null)
                    leftPageText.text = ScrambleText(targetLeft, leftBuffer, progress);

                if (rightPageText != null)
                    rightPageText.text = ScrambleText(targetRight, rightBuffer, progress);

                yield return new WaitForSeconds(0.02f);
            }

            // Snap cleanly to final text and scale
            if (leftRect != null) leftRect.localScale = Vector3.one;
            if (rightRect != null) rightRect.localScale = Vector3.one;
            if (bookSpreadRect != null) bookSpreadRect.localRotation = Quaternion.identity;

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
                {
                    buffer[i] = c;
                }
                else if (i < revealedCount)
                {
                    buffer[i] = c;
                }
                else
                {
                    buffer[i] = GLITCH_CHARS[Random.Range(0, GLITCH_CHARS.Length)];
                }
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
                string entry = !string.IsNullOrEmpty(list[i].bookStudyNote)
                    ? list[i].bookStudyNote
                    : $"{list[i].questionText} -> {list[i].correctTextAnswer}";

                sb.AppendLine($"• {entry}\n");
            }
            return sb.ToString();
        }

        // ==========================================
        // 3. CLOSE BOOK
        // ==========================================
        public void CloseBook()
        {
            if (!IsBookOpen) return;
            IsBookOpen = false;
            StopAllCoroutines();

            if (bookUIPanel != null) bookUIPanel.SetActive(false);

            if (current3DBook != null)
            {
                current3DBook.PutDownBook();
            }

            if (ExamUIManager.Instance != null && !ExamUIManager.Instance.IsChitOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}