using UnityEngine;
using TMPro;
using System.Collections;

namespace FlamexStudios.Exam
{
    public class CurriculumManager : MonoBehaviour
    {
        public static CurriculumManager Instance { get; private set; }

        [Header("Progression State")]
        [Tooltip("1 = Spelling Bee, 2-5 = Classrooms, 6-7 = Basketball Court")]
        public int currentUnlockedTest = 1;
        public int totalTestsToEscape = 7;

        [Header("10-Minute Prep Timer")]
        public float prepTimeSeconds = 600f; // 10 minutes
        public bool isTimerRunning = false;
        private float currentTimer;

        [Header("HUD & Trap Popup UI")]
        public GameObject trapIntroPopup;
        public TextMeshProUGUI timerTextUI;
        public TextMeshProUGUI objectiveTextUI;
        public GameObject accessDeniedPopup;
        public AudioSource managerAudioSource;
        public AudioClip accessDeniedSFX;
        public AudioClip testUnlockedSFX;

        // Static variable survives scene reloads to show the Library Hint after dying!
        public static bool ShowLibraryHintOnReload = false;
        public GameObject libraryHintBanner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            currentTimer = prepTimeSeconds;

            // If player died previously, show the Library hint!
            if (ShowLibraryHintOnReload && libraryHintBanner != null)
            {
                libraryHintBanner.SetActive(true);
            }

            UpdateObjectiveHUD();
            StartCoroutine(TriggerIntroTrapSequence());
        }

        private IEnumerator TriggerIntroTrapSequence()
        {
            if (trapIntroPopup != null)
            {
                trapIntroPopup.SetActive(true);
                yield return new WaitForSeconds(5f);
                trapIntroPopup.SetActive(false);
            }
            isTimerRunning = true;
        }

        private void Update()
        {
            // Pause the slacking timer while the player is actively sitting in a test
            if (!isTimerRunning || (ExamUIManager.Instance != null && ExamUIManager.Instance.currentState != ExamState.Inactive))
                return;

            currentTimer -= Time.deltaTime;
            UpdateTimerUI();

            if (currentTimer <= 0f)
            {
                isTimerRunning = false;
                TriggerSlackingDeath();
            }
        }

        private void UpdateTimerUI()
        {
            if (timerTextUI == null) return;
            int minutes = Mathf.FloorToInt(Mathf.Max(0, currentTimer) / 60f);
            int seconds = Mathf.FloorToInt(Mathf.Max(0, currentTimer) % 60f);
            timerTextUI.text = string.Format("NEXT EXAM IN: {0:00}:{1:00}", minutes, seconds);
        }

        public void UpdateObjectiveHUD()
        {
            if (objectiveTextUI == null) return;

            if (currentUnlockedTest <= 5)
                objectiveTextUI.text = $"OBJECTIVE: Pass Class {currentUnlockedTest} Exam ({currentUnlockedTest}/{totalTestsToEscape})";
            else if (currentUnlockedTest <= 7)
                objectiveTextUI.text = $"OBJECTIVE: Complete Court Trial {currentUnlockedTest - 5} ({currentUnlockedTest}/{totalTestsToEscape})";
            else
                objectiveTextUI.text = "OBJECTIVE: CURRICULUM COMPLETE - ESCAPE THE SCHOOL";
        }

        public void OnExamPassed()
        {
            currentUnlockedTest++;
            currentTimer = prepTimeSeconds; // Reset 10-min timer for next class
            isTimerRunning = true;

            if (managerAudioSource != null && testUnlockedSFX != null)
                managerAudioSource.PlayOneShot(testUnlockedSFX);

            UpdateObjectiveHUD();
        }

        public void TriggerAccessDenied()
        {
            if (managerAudioSource != null && accessDeniedSFX != null)
                managerAudioSource.PlayOneShot(accessDeniedSFX);

            if (accessDeniedPopup != null)
            {
                StopCoroutine(nameof(HideAccessDeniedRoutine));
                StartCoroutine(nameof(HideAccessDeniedRoutine));
            }
        }

        private IEnumerator HideAccessDeniedRoutine()
        {
            accessDeniedPopup.SetActive(true);
            yield return new WaitForSeconds(2f);
            accessDeniedPopup.SetActive(false);
        }

        private void TriggerSlackingDeath()
        {
            ShowLibraryHintOnReload = true;
            Debug.Log("TIME EXPIRED! Ghost spawns: 'Why didn't you give the test?!'");
            // Call GhostAIManager.Instance.TriggerSlackingCutscene() here!
        }
    }
}