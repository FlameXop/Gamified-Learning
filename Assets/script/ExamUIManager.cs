using FlamexStudios.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FlamexStudios.Exam
{
    public class ExamUIManager : MonoBehaviour
    {
        public static ExamUIManager Instance { get; private set; }

        [Header("UI Layers (Assign in Inspector)")]
        public GameObject welcomeScreenPanel;
        public CanvasGroup welcomeCanvasGroup;  // NEW: Controls Welcome Screen fade
        public GameObject mainTestPanel;
        public GameObject chitPanel;
        public GameObject hudPanel;

        [Header("Tablet Animation Settings (Main Test)")]
        public RectTransform tabletPanel;       // The physical tablet UI box
        public CanvasGroup tabletCanvasGroup;   // Controls the fade-in transparency

        [Header("Chit Components")]
        public TMP_InputField chitInputField;

        [Header("HUD Elements")]
        public Slider awarenessBar;
        public Slider trustMeter;
        public GameObject trustPromptUI;

        [Header("Test UI Containers")]
        public GameObject mcqContainer;
        public GameObject spellingContainer;
        public TMP_InputField spellingInputField;
        public AudioSource examAudioSource;

        [Header("Dynamic Test Data")]
        public TextMeshProUGUI welcomeTitleText;
        public TextMeshProUGUI[] answerButtonTexts;

        // Internal State
        private ExamData currentExamData;
        private DeskInteractable currentDesk;
        private int currentQuestionIndex = 0;
        private int correctAnswersCount = 0;

        public bool IsChitOpen { get; private set; }
        public bool IsTestActive { get; private set; }
        private bool wasChitTogglePressed = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            if (chitInputField != null) chitInputField.characterLimit = 500;
        }

        // ==========================================
        // 1. WELCOME SCREEN LOGIC
        // ==========================================
        public void TriggerWelcomeScreen(ExamData examData, DeskInteractable desk)
        {
            currentExamData = examData;
            currentDesk = desk;
            currentQuestionIndex = 0;
            correctAnswersCount = 0;

            if (welcomeTitleText != null)
                welcomeTitleText.text = currentExamData.examTitle;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            welcomeScreenPanel.SetActive(true);

            // Trigger the smooth fade-in for the Welcome Screen
            StartCoroutine(FadeInWelcomeScreen());
        }

        private IEnumerator FadeInWelcomeScreen()
        {
            float duration = 0.5f;
            float elapsed = 0f;

            if (welcomeCanvasGroup != null) welcomeCanvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (welcomeCanvasGroup != null)
                    welcomeCanvasGroup.alpha = elapsed / duration;

                yield return null;
            }
            if (welcomeCanvasGroup != null) welcomeCanvasGroup.alpha = 1f;
        }

        // ==========================================
        // 2. MAIN TEST & TABLET ANIMATION LOGIC
        // ==========================================
        public void StartActualTest()
        {
            welcomeScreenPanel.SetActive(false);
            mainTestPanel.SetActive(true);
            hudPanel.SetActive(true);
            IsTestActive = true;

            StartCoroutine(AnimateTabletIn());

        }

        // ==========================================
        private IEnumerator AnimateTabletIn()
        {
            float duration = 0.6f;
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0, -500); // Start lower on screen
            Vector2 endPos = new Vector2(0, 0);

            tabletCanvasGroup.alpha = 0f;
            tabletPanel.localScale = new Vector3(0.2f, 0.2f, 0.2f); // Start tiny!

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // AAA Math: "Ease Out Back" for that bouncy macOS overshoot effect
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float bounceMath = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

                // Apply position and scale bounce
                tabletPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, bounceMath);
                tabletPanel.localScale = Vector3.Lerp(new Vector3(0.2f, 0.2f, 0.2f), Vector3.one, bounceMath);

                // Alpha fades in normally
                float alphaMath = 1f - Mathf.Pow(1f - t, 3f);
                tabletCanvasGroup.alpha = Mathf.Lerp(0f, 1f, alphaMath);

                yield return null;
            }

            tabletPanel.anchoredPosition = endPos;
            tabletPanel.localScale = Vector3.one;
            tabletCanvasGroup.alpha = 1f;

            LoadNextQuestion();
        }

        // ==========================================
        // 3. QUESTION HANDLING LOGIC
        // ==========================================
        public void LoadNextQuestion()
        {
            if (currentQuestionIndex >= currentExamData.questions.Count)
            {
                FinishExam();
                return;
            }

            Question q = currentExamData.questions[currentQuestionIndex];
            welcomeTitleText.text = q.questionText; // Reusing this text to show the question

            if (q.type == QuestionType.AudioSpelling)
            {
                mcqContainer.SetActive(false);
                spellingContainer.SetActive(true);
                spellingInputField.text = "";

                if (q.spokenAudio != null && examAudioSource != null)
                {
                    examAudioSource.PlayOneShot(q.spokenAudio);
                }
            }
            else if (q.type == QuestionType.MultipleChoice)
            {
                spellingContainer.SetActive(false);
                mcqContainer.SetActive(true);

                for (int i = 0; i < 4; i++)
                {
                    answerButtonTexts[i].text = q.answers[i];
                }
            }
        }

        // ==========================================
        // 4. ANSWER SUBMISSIONS
        // ==========================================
        public void SubmitSpellingAnswer()
        {
            Question q = currentExamData.questions[currentQuestionIndex];
            string playerTyping = spellingInputField.text.Trim().ToLower();
            string correctAnswer = q.correctTextAnswer.Trim().ToLower();

            if (playerTyping == correctAnswer)
            {
                correctAnswersCount++;
            }
            else
            {
                AddAwareness(10f);
            }

            currentQuestionIndex++;
            LoadNextQuestion();
        }

        public void SubmitAnswer(int buttonIndex)
        {
            Question q = currentExamData.questions[currentQuestionIndex];

            if (buttonIndex == q.correctAnswerIndex)
            {
                correctAnswersCount++;
            }
            else
            {
                AddAwareness(10f);
            }

            currentQuestionIndex++;
            LoadNextQuestion();
        }

        public void ReplaySpellingAudio()
        {
            Question q = currentExamData.questions[currentQuestionIndex];
            if (q.spokenAudio != null && examAudioSource != null)
            {
                examAudioSource.PlayOneShot(q.spokenAudio);
            }
        }

        // ==========================================
        // 5. GAMEPLAY LOOP & PENALTIES
        // ==========================================
        private void FinishExam()
        {
            IsTestActive = false;
            float scorePercentage = (float)correctAnswersCount / currentExamData.questions.Count;

            if (scorePercentage >= 0.5f)
            {
                Debug.Log("Exam Passed!");
                PlayerManager.Instance.SetMeshVisibility(true);
                mainTestPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // Add your logic to switch camera back to 3rd person and re-enable movement here!
            }
            else
            {
                TriggerDeathSequence("You failed the test.");
            }
        }

        public void AddAwareness(float amount)
        {
            if (awarenessBar != null)
            {
                awarenessBar.value += amount;
                if (awarenessBar.value >= 100f)
                {
                    TriggerDeathSequence("The Ghost caught you cheating!");
                }
            }
        }

        private void TriggerDeathSequence(string reason)
        {
            IsTestActive = false;
            Debug.Log("GHOST KILLS YOU: " + reason);
            // Put Jumpscare / Reload Scene here
        }

        private void Update()
        {
            // 1. TOGGLE LOGIC (Press once to open, press again to close)
            bool isTogglePressed = PlayerManager.Instance.Input.ChitToggleTriggered;

            if (isTogglePressed && !wasChitTogglePressed)
            {
                IsChitOpen = !IsChitOpen; // Flip the state
                chitPanel.SetActive(IsChitOpen);

                if (IsChitOpen)
                {
                    // Force the keyboard to type into the Chit Panel!
                    if (chitInputField != null)
                    {
                        chitInputField.Select();
                        chitInputField.ActivateInputField();
                    }
                }
                else
                {
                    // If closed during a spelling test, give typing focus back to the test!
                    if (IsTestActive && currentExamData != null && currentExamData.questions[currentQuestionIndex].type == QuestionType.AudioSpelling)
                    {
                        if (spellingInputField != null)
                        {
                            spellingInputField.Select();
                            spellingInputField.ActivateInputField();
                        }
                    }
                }
            }
            wasChitTogglePressed = isTogglePressed;

            // 2. AGGRESSIVE MOUSE UNLOCK
            // If the test is running OR the chit is open, the mouse MUST be free.
            if (IsTestActive || IsChitOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}