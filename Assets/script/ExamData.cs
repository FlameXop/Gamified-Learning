using System.Collections.Generic;
using UnityEngine;

namespace FlamexStudios.Exam
{
    public enum QuestionType
    {
        AudioSpelling,   // Used for Class 1 (Spelling Bee)
        MultipleChoice   // Used for Classes 2-7 (4 Custom Options)
    }

    [System.Serializable]
    public class Question
    {
        public QuestionType type = QuestionType.MultipleChoice;

        [Header("1. What Appears in the Library Book")]
        [TextArea(2, 4)]
        [Tooltip("Study material displayed on the Library Book pages")]
        public string bookStudyNote;

        [Header("2. What Appears on the Exam Tablet")]
        [TextArea(2, 3)]
        public string questionText;

        [Header("3A. Multiple Choice Settings (Classes 2-7)")]
        [Tooltip("Type the 4 exact options to display on buttons A, B, C, and D")]
        public string[] mcqOptions = new string[4];

        [Range(0, 3)]
        [Tooltip("0 = Button A, 1 = Button B, 2 = Button C, 3 = Button D")]
        public int correctOptionIndex = 0;

        [Header("3B. Spelling Bee Settings (Class 1 Only)")]
        [Tooltip("Exact word the player must type for AudioSpelling questions")]
        public string correctTextAnswer;
        public AudioClip spokenAudio;
    }

    [CreateAssetMenu(fileName = "NewExamData", menuName = "FlamexStudios/Exam Data")]
    public class ExamData : ScriptableObject
    {
        public int classNumber = 1;
        public string examTitle = "Class 1: Spelling Bee";
        public List<Question> questions = new List<Question>();
    }
}