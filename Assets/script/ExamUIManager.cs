using FlamexStudios.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace FlamexStudios.Exam
{
    public enum ExamState
    {
        Inactive,        // Walking around the school
        Transitioning,   // Camera is panning to the desk
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
        public bool keepHudVisibleOutsideTests = true;
        public Slider awarenessBar;
        public Slider trustMeter;
        public GameObject trustPromptUI;
        public float CurrentAwareness { get; private set; } = 0f;

        [Header("Tablet Animation Settings (Main Test)")]
        public RectTransform tabletPanel;
        public CanvasGroup tabletCanvasGroup;

        [Header("Chit Components & Animation")]
        public TMP_InputField chitInputField;
        public RectTransform chitRectTransform;
        public CanvasGroup chitCanvasGroup;

        [Header("Test UI Containers")]
        public GameObject mcqContainer;
        public GameObject spellingContainer;
        public TMP_InputField spellingInputField;
        public AudioSource examAudioSource;

        [Header("Dynamic Text Elements")]
        public TextMeshProUGUI welcomeTitleText;
        public TextMeshProUGUI questionTextUI;
        public TextMeshProUGUI[] answerButtonTexts;

        [Header("Dynamic Question Generation")]
        public int maxQuestionsPerTest = 10;

        // Internal Exam State
        private ExamData currentExamData;
        private List<Question> runtimeQuestions = new List<Question>();
        private DeskInteractable currentDesk;
        private int currentQuestionIndex = 0;
        private int correctAnswersCount = 0;

        // Coroutine Trackers
        private Coroutine chitAnimCoroutine;
        private Coroutine panelGlitchCoroutine;
        private Coroutine textDecodeCoroutine;

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

            SetupChitReferences();

            IsChitOpen = false;
            if (chitPanel != null) chitPanel.SetActive(false);

            // Force BookPanel off at start if it wasn't being read
            if (LibraryBookUIManager.Instance != null && !LibraryBookUIManager.Instance.IsBookOpen && LibraryBookUIManager.Instance.bookUIPanel != null)
            {
                LibraryBookUIManager.Instance.bookUIPanel.SetActive(false);
            }

            SetExamState(ExamState.Inactive);
        }

        private void SetupChitReferences()
        {
            if (chitPanel == null) return;

            if (chitRectTransform == null)
                chitRectTransform = chitPanel.GetComponent<RectTransform>();

            if (chitCanvasGroup == null)
            {
                chitCanvasGroup = chitPanel.GetComponent<CanvasGroup>();
                if (chitCanvasGroup == null)
                    chitCanvasGroup = chitPanel.AddComponent<CanvasGroup>();
            }
        }

        // ==========================================
        // CENTRAL EXAM STATE MACHINE
        // ==========================================
        public void SetExamState(ExamState newState)
        {
            currentState = newState;

            if (panelGlitchCoroutine != null) StopCoroutine(panelGlitchCoroutine);
            if (textDecodeCoroutine != null) StopCoroutine(textDecodeCoroutine);

            switch (currentState)
            {
                case ExamState.Inactive:
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(false);
                    if (mainTestPanel != null) mainTestPanel.SetActive(false);
                    if (hudPanel != null) hudPanel.SetActive(keepHudVisibleOutsideTests);
                    break;

                case ExamState.Transitioning:
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(false);
                    if (mainTestPanel != null) mainTestPanel.SetActive(false);
                    break;

                case ExamState.WelcomeScreen:
                    if (mainTestPanel != null) mainTestPanel.SetActive(false);
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(true);
                    break;

                case ExamState.InTest:
                    if (welcomeScreenPanel != null) welcomeScreenPanel.SetActive(false);
                    if (mainTestPanel != null) mainTestPanel.SetActive(true);
                    if (hudPanel != null) hudPanel.SetActive(true);
                    break;
            }

            RefreshCursorAndMovement();
        }

        // Locks WASD/Jump & unlocks mouse whenever Chit, Book, or Test is open!
        public void RefreshCursorAndMovement()
        {
            bool isBookOpen = LibraryBookUIManager.Instance != null && LibraryBookUIManager.Instance.IsBookOpen;
            bool anyUIOpen = IsChitOpen || isBookOpen || currentState != ExamState.Inactive;

            if (anyUIOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Freeze player movement & jumping while typing in Chit, reading Book, or sitting in Exam
            if (PlayerManager.Instance != null && PlayerManager.Instance.Movement != null)
            {
                PlayerManager.Instance.Movement.enabled = !anyUIOpen;
            }
        }

        // ==========================================
        // INDEPENDENT CHIT SYSTEM (ONLY TOUCHES CHIT_PANEL)
        // ==========================================
        public void ToggleChit()
        {
            if (chitPanel == null) return;

            SetupChitReferences();
            IsChitOpen = !IsChitOpen;

            if (chitAnimCoroutine != null)
                StopCoroutine(chitAnimCoroutine);

            // Guarantee BookPanel NEVER turns on from pressing Tab if we aren't reading a book!
            if (LibraryBookUIManager.Instance != null && !LibraryBookUIManager.Instance.IsBookOpen && LibraryBookUIManager.Instance.bookUIPanel != null)
            {
                LibraryBookUIManager.Instance.bookUIPanel.SetActive(false);
            }

            if (IsChitOpen)
            {
                chitPanel.SetActive(true);
                chitPanel.transform.SetAsLastSibling();
            }

            chitAnimCoroutine = StartCoroutine(AnimateChitRoutine(IsChitOpen));

            // Focus or unfocus the TMP_InputField so WASD/Space only types in the box
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
                if (chitInputField != null)
                {
                    chitInputField.DeactivateInputField();
                }

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

            RefreshCursorAndMovement();
        }

        private IEnumerator AnimateChitRoutine(bool opening)
        {
            float duration = 0.22f;
            float elapsed = 0f;

            Vector3 startScale = opening ? new Vector3(0.75f, 0.75f, 1f) : Vector3.one;
            Vector3 endScale = opening ? Vector3.one : new Vector3(0.75f, 0.75f, 1f);

            float startAlpha = opening ? 0f : (chitCanvasGroup != null ? chitCanvasGroup.alpha : 1f);
            float endAlpha = opening ? 1f : 0f;

            float startTilt = opening ? -6f : 0f;
            float endTilt = opening ? 0f : -6f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                if (chitRectTransform != null)
                {
                    chitRectTransform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
                    chitRectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startTilt, endTilt, smoothT));
                }

                if (chitCanvasGroup != null)
                {
                    chitCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, smoothT);
                }

                yield return null;
            }

            if (chitRectTransform != null)
            {
                chitRectTransform.localScale = endScale;
                chitRectTransform.localRotation = Quaternion.Euler(0f, 0f, endTilt);
            }

            if (chitCanvasGroup != null)
                chitCanvasGroup.alpha = endAlpha;

            if (!opening && chitPanel != null)
            {
                chitPanel.SetActive(false);
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

            GenerateDynamicQuestionSet();
            SetExamState(ExamState.WelcomeScreen);

            RectTransform welcomeRect = welcomeScreenPanel != null ? welcomeScreenPanel.GetComponent<RectTransform>() : null;
            panelGlitchCoroutine = StartCoroutine(CyberpunkGlitchIn(welcomeRect, welcomeCanvasGroup, () =>
            {
                if (welcomeTitleText != null && currentExamData != null)
                {
                    textDecodeCoroutine = StartCoroutine(DecodeTextEffect(welcomeTitleText, currentExamData.examTitle, 0.35f));
                }
            }));
        }

        private void GenerateDynamicQuestionSet()
        {
            runtimeQuestions.Clear();
            if (currentExamData == null || currentExamData.questions == null) return;

            List<Question> pool = new List<Question>(currentExamData.questions);

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                Question temp = pool[i];
                pool[i] = pool[r];
                pool[r] = temp;
            }

            int countToTake = Mathf.Min(maxQuestionsPerTest, pool.Count);

            for (int i = 0; i < countToTake; i++)
            {
                Question q = pool[i];

                if (q.type == QuestionType.MultipleChoice)
                {
                    List<string> options = new List<string> { q.correctTextAnswer };

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

                    while (options.Count < 4) options.Add($"Option {options.Count + 1}");

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

            panelGlitchCoroutine = StartCoroutine(CyberpunkGlitchIn(tabletPanel, tabletCanvasGroup, () =>
            {
                LoadNextQuestion();
            }));
        }

        private IEnumerator CyberpunkGlitchIn(RectTransform targetRect, CanvasGroup targetGroup, System.Action onComplete)
        {
            float duration = 0.28f;
            float elapsed = 0f;

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
                }

                if (targetGroup != null)
                {
                    targetGroup.alpha = (Random.value > 0.25f) ? Mathf.Lerp(0.3f, 1f, t) : 0.1f;
                }

                yield return new WaitForSeconds(0.02f);
                elapsed += 0.02f;
            }

            if (targetRect != null) targetRect.localScale = Vector3.one;
            if (targetGroup != null) targetGroup.alpha = 1f;

            if (IsChitOpen && chitPanel != null)
                chitPanel.transform.SetAsLastSibling();

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
                textDecodeCoroutine = StartCoroutine(DecodeTextEffect(activeQuestionText, q.questionText, 0.35f));
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
        // 5. GAMEPLAY LOOP & GLOBAL GHOST AWARENESS
        // ==========================================
        private void FinishExam()
        {
            SetExamState(ExamState.Inactive);

            float scorePercentage = (float)correctAnswersCount / runtimeQuestions.Count;

            if (scorePercentage >= 0.5f)
            {
                Debug.Log("Exam Passed!");
                if (currentDesk != null)
                {
                    currentDesk.CompleteAndStandUp();
                }
                else
                {
                    PlayerManager.Instance.SetMeshVisibility(true);
                    RefreshCursorAndMovement();
                }
            }
            else
            {
                TriggerDeathSequence("You failed the test.");
            }
        }

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