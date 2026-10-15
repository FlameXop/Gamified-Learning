using FlamexStudios.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FlamexStudios.Exam
{
    public enum ExamState
    {
        Inactive,        // Walking around the school
        Transitioning,   // Camera is panning to the desk (locks all inputs)
        WelcomeScreen,   // Sitting at desk, looking at "Start Test" screen
        InTest           // Actively answering questions on the tablet
    }

    public class ExamUIManager : MonoBehaviour
    {
        public static ExamUIManager Instance { get; private set; }

        [Header("Current State (Read Only)")]
        public ExamState currentState = ExamState.Inactive;

        [Header("UI Layers (Assign in Inspector)")]
        public GameObject welcomeScreenPanel;
        public CanvasGroup welcomeCanvasGroup;
        public GameObject mainTestPanel;
        public GameObject chitPanel;
        public GameObject hudPanel;

        [Header("HUD & Ghost AI Settings")]
        [Tooltip("Keep true so Ghost AI can raise awareness when slacking off outside tests!")]
        public bool keepHudVisibleOutsideTests = true;
        public Slider awarenessBar;
        public Slider trustMeter;
        public GameObject trustPromptUI;
        public float CurrentAwareness { get; private set; } = 0f;

        [Header("Tablet Animation Settings (Main Test)")]
        public RectTransform tabletPanel;
        public CanvasGroup tabletCanvasGroup;

        [Header("Chit Components")]
        public TMP_InputField chitInputField;

        [Header("Test UI Containers")]
        public GameObject mcqContainer;
        public GameObject spellingContainer;
        public TMP_InputField spellingInputField;
        public AudioSource examAudioSource;

        [Header("Dynamic Text Elements")]
        public TextMeshProUGUI welcomeTitleText; // Text inside WelcomeScreen_Panel
        public TextMeshProUGUI questionTextUI;   // Text inside MainTest_Panel (Tablet)
        public TextMeshProUGUI[] answerButtonTexts;

        [Header("Dynamic Question Generation")]
        [Tooltip("How many questions to randomly pull from the ExamData pool per test")]
        public int maxQuestionsPerTest = 10;

        // Internal State
        private ExamData currentExamData;
        private List<Question> runtimeQuestions = new List<Question>();
        private DeskInteractable currentDesk;
        private int currentQuestionIndex = 0;
        private int correctAnswersCount = 0;

        private const string GLITCH_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%&*<>[]/_";

        public bool IsChitOpen { get; private set; }
        public bool IsTestActive => currentState == ExamState.InTest;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (chitInputField != null) chitInputField.characterLimit = 500;

            IsChitOpen = false;
            if (chitPanel != null) chitPanel.SetActive(false);
            SetExamState(ExamState.Inactive);
        }

        // ==========================================
        // CENTRAL STATE MACHINE
        // ==========================================
        public void SetExamState(ExamState newState)
        {
            currentState = newState;
            StopAllCoroutines();

            switch (currentState)
            {
                case ExamState.Inactive:
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(false);
                    if (mainTestPanel != null) mainTestPanel.SetActive(false);
                    if (hudPanel != null) hudPanel.SetActive(keepHudVisibleOutsideTests);
                    break;

                case ExamState.Transitioning:
                    // Locks Chit and hides panels while camera sweeps to desk
                    CloseChit();
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(false);
                    if (mainTestPanel != null) mainTestPanel.SetActive(false);
                    break;

                case ExamState.WelcomeScreen:
                    CloseChit();
                    if (mainTestPanel != null) mainTestPanel.SetActive(false);
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(true);
                    break;

                case ExamState.InTest:
                    CloseChit();
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(false);
                    if (mainTestPanel != null) mainTestPanel.SetActive(true);
                    if (hudPanel != null) hudPanel.SetActive(true);
                    break;
            }

            UpdateCursorState();
        }

        private void UpdateCursorState()
        {
            if (currentState == ExamState.WelcomeScreen || currentState == ExamState.InTest || IsChitOpen)
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

        // ==========================================
        // 1. WELCOME SCREEN & DYNAMIC POOL GENERATION
        // ==========================================
        public void TriggerWelcomeScreen(ExamData examData, DeskInteractable desk)
        {
            currentExamData = examData;
            currentDesk = desk;
            currentQuestionIndex = 0;
            correctAnswersCount = 0;

            // Dynamically generate & shuffle up to 10 questions for this run
            GenerateDynamicQuestionSet();

            SetExamState(ExamState.WelcomeScreen);

            RectTransform welcomeRect = welcomeScreenPanel != null ? welcomeScreenPanel.GetComponent<RectTransform>() : null;
            StartCoroutine(CyberpunkGlitchIn(welcomeRect, welcomeCanvasGroup, () =>
            {
                if (welcomeTitleText != null && currentExamData != null)
                {
                    StartCoroutine(DecodeTextEffect(welcomeTitleText, currentExamData.examTitle, 0.35f));
                }
            }));
        }

        private void GenerateDynamicQuestionSet()
        {
            runtimeQuestions.Clear();
            if (currentExamData == null || currentExamData.questions == null) return;

            List<Question> pool = new List<Question>(currentExamData.questions);

            // 1. Shuffle the questions for this test run
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                Question temp = pool[i];
                pool[i] = pool[r];
                pool[r] = temp;
            }

            int countToTake = Mathf.Min(maxQuestionsPerTest, pool.Count);

            // 2. Auto-build 4 MCQ options for each selected question using answers from OTHER questions in the book!
            for (int i = 0; i < countToTake; i++)
            {
                Question q = pool[i];

                if (q.type == QuestionType.MultipleChoice)
                {
                    List<string> options = new List<string> { q.correctTextAnswer };

                    // Grab 3 random wrong answers (distractors) from the rest of the book
                    int safetyCounter = 0;
                    while (options.Count < 4 && safetyCounter < 100)
                    {
                        safetyCounter++;
                        string randomWrong = pool[Random.Range(0, pool.Count)].correctTextAnswer;
                        if (!options.Contains(randomWrong) && !string.IsNullOrEmpty(randomWrong))
                        {
                            options.Add(randomWrong);
                        }
                    }

                    // Fallback if the book has fewer than 4 unique answers
                    while (options.Count < 4) options.Add($"Option {options.Count + 1}");

                    // Shuffle the 4 MCQ options and record which button holds the right answer
                    for (int j = options.Count - 1; j > 0; j--)
                    {
                        int r = Random.Range(0, j + 1);
                        string tempOpt = options[j];
                        options[j] = options[r];
                        options[r] = tempOpt;
                    }

                    q.runtimeMcqOptions = options.ToArray();
                    q.runtimeCorrectButtonIndex = options.IndexOf(q.correctTextAnswer);
                }

                runtimeQuestions.Add(q);
            }
        }

        // ==========================================
        // 2. MAIN TEST & CYBERPUNK GLITCH ANIMATION
        // ==========================================
        public void StartActualTest()
        {
            SetExamState(ExamState.InTest);

            if (questionTextUI != null) questionTextUI.text = "";
            else if (welcomeTitleText != null) welcomeTitleText.text = "";

            StartCoroutine(CyberpunkGlitchIn(tabletPanel, tabletCanvasGroup, () =>
            {
                LoadNextQuestion();
            }));
        }

        private IEnumerator CyberpunkGlitchIn(RectTransform targetRect, CanvasGroup targetGroup, System.Action onComplete)
        {
            float duration = 0.28f;
            float elapsed = 0f;
            Vector2 finalPos = Vector2.zero;

            if (targetRect != null)
            {
                targetRect.localScale = new Vector3(1.05f, 0.02f, 1f);
            }
            if (targetGroup != null) targetGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (targetRect != null)
                {
                    float scaleY = Mathf.Lerp(0.02f, 1f, Mathf.Pow(t, 0.4f));
                    float scaleX = (Random.value > 0.7f) ? Random.Range(0.96f, 1.06f) : 1f;
                    targetRect.localScale = new Vector3(scaleX, scaleY, 1f);

                    float jitterX = (t < 0.8f) ? Random.Range(-12f, 12f) : 0f;
                    float jitterY = (t < 0.8f) ? Random.Range(-3f, 3f) : 0f;
                    targetRect.anchoredPosition = finalPos + new Vector2(jitterX, jitterY);
                }

                if (targetGroup != null)
                {
                    targetGroup.alpha = (Random.value > 0.25f) ? Mathf.Lerp(0.3f, 1f, t) : 0.1f;
                }

                yield return new WaitForSeconds(0.02f);
                elapsed += 0.02f;
            }

            if (targetRect != null)
            {
                targetRect.localScale = Vector3.one;
                targetRect.anchoredPosition = finalPos;
            }
            if (targetGroup != null) targetGroup.alpha = 1f;

            onComplete?.Invoke();
        }

        private IEnumerator DecodeTextEffect(TextMeshProUGUI textComponent, string finalText, float duration = 0.35f)
        {
            if (textComponent == null || string.IsNullOrEmpty(finalText)) yield break;

            float elapsed = 0f;
            char[] buffer = new char[finalText.Length];

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                int revealedCount = Mathf.FloorToInt(progress * finalText.Length);

                for (int i = 0; i < finalText.Length; i++)
                {
                    if (finalText[i] == ' ')
                        buffer[i] = ' ';
                    else if (i < revealedCount)
                        buffer[i] = finalText[i];
                    else
                        buffer[i] = GLITCH_CHARS[Random.Range(0, GLITCH_CHARS.Length)];
                }

                textComponent.text = new string(buffer);
                yield return new WaitForSeconds(0.02f);
                elapsed += 0.02f;
            }

            textComponent.text = finalText;
        }

        // ==========================================
        // 3. QUESTION HANDLING LOGIC
        // ==========================================
        public void LoadNextQuestion()
        {
            if (currentQuestionIndex >= runtimeQuestions.Count)
            {
                FinishExam();
                return;
            }

            Question q = runtimeQuestions[currentQuestionIndex];

            TextMeshProUGUI activeQuestionText = (questionTextUI != null) ? questionTextUI : welcomeTitleText;
            if (activeQuestionText != null)
            {
                StartCoroutine(DecodeTextEffect(activeQuestionText, q.questionText, 0.35f));
            }

            if (q.type == QuestionType.AudioSpelling)
            {
                mcqContainer.SetActive(false);
                spellingContainer.SetActive(true);
                spellingInputField.text = "";

                if (!IsChitOpen && spellingInputField != null)
                {
                    spellingInputField.Select();
                    spellingInputField.ActivateInputField();
                }

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
                    StartCoroutine(DecodeTextEffect(answerButtonTexts[i], q.runtimeMcqOptions[i], 0.25f));
                }
            }
        }

        // ==========================================
        // 4. ANSWER SUBMISSIONS
        // ==========================================
        public void SubmitSpellingAnswer()
        {
            Question q = runtimeQuestions[currentQuestionIndex];
            string playerTyping = spellingInputField.text.Trim().ToLower();
            string correctAnswer = q.correctTextAnswer.Trim().ToLower();

            if (playerTyping == correctAnswer)
                correctAnswersCount++;
            else
                AddAwareness(10f);

            currentQuestionIndex++;
            LoadNextQuestion();
        }

        public void SubmitAnswer(int buttonIndex)
        {
            Question q = runtimeQuestions[currentQuestionIndex];

            if (buttonIndex == q.runtimeCorrectButtonIndex)
                correctAnswersCount++;
            else
                AddAwareness(10f);
            currentQuestionIndex++;
            LoadNextQuestion();
        }

        public void ReplaySpellingAudio()
        {
            Question q = runtimeQuestions[currentQuestionIndex];
            if (q.spokenAudio != null && examAudioSource != null)
            {
                examAudioSource.PlayOneShot(q.spokenAudio);
            }
        }

        // ==========================================
        // 5. CHIT SYSTEM LOGIC
        // ==========================================
        private void ToggleChit()
        {
            // Block Chit from opening during camera transitions or Welcome Screen
            if (currentState == ExamState.Transitioning || currentState == ExamState.WelcomeScreen) return;

            IsChitOpen = !IsChitOpen;
            if (chitPanel != null) chitPanel.SetActive(IsChitOpen);

            if (IsChitOpen)
            {
                if (chitInputField != null)
                {
                    chitInputField.Select();
                    chitInputField.ActivateInputField();
                }
            }
            else
            {
                if (currentState == ExamState.InTest && runtimeQuestions.Count > 0 &&
                    currentQuestionIndex < runtimeQuestions.Count &&
                    runtimeQuestions[currentQuestionIndex].type == QuestionType.AudioSpelling)
                {
                    if (spellingInputField != null)
                    {
                        spellingInputField.Select();
                        spellingInputField.ActivateInputField();
                    }
                }
            }

            UpdateCursorState();
        }

        private void CloseChit()
        {
            IsChitOpen = false;
            if (chitPanel != null) chitPanel.SetActive(false);
            if (PlayerManager.Instance != null && PlayerManager.Instance.Input != null)
            {
                PlayerManager.Instance.Input.ConsumeChitToggle();
            }
        }

        // ==========================================
        // 6. GAMEPLAY LOOP & GLOBAL GHOST AWARENESS
        // ==========================================
        private void FinishExam()
        {
            CloseChit();
            SetExamState(ExamState.Inactive);

            float scorePercentage = (float)correctAnswersCount / runtimeQuestions.Count;

            if (scorePercentage >= 0.5f)
            {
                Debug.Log("Exam Passed!");
                PlayerManager.Instance.SetMeshVisibility(true);
                PlayerManager.Instance.Movement.enabled = true;
            }
            else
            {
                TriggerDeathSequence("You failed the test.");
            }
        }

        // Works globally! Ghost AI can call ExamUIManager.Instance.AddAwareness(amount) anytime
        public void AddAwareness(float amount)
        {
            CurrentAwareness = Mathf.Clamp(CurrentAwareness + amount, 0f, 100f);

            if (awarenessBar != null)
            {
                awarenessBar.value = CurrentAwareness;
            }

            if (CurrentAwareness >= 100f)
            {
                TriggerDeathSequence("The Ghost caught you slacking or cheating!");
            }
        }

        private void TriggerDeathSequence(string reason)
        {
            CloseChit();
            SetExamState(ExamState.Inactive);
            Debug.Log("GHOST KILLS YOU: " + reason);
        }

        private void Update()
        {
            if (PlayerManager.Instance == null || PlayerManager.Instance.Input == null) return;

            if (PlayerManager.Instance.Input.ChitToggleTriggered)
            {
                PlayerManager.Instance.Input.ConsumeChitToggle();
                ToggleChit();
            }
        }
    }
}