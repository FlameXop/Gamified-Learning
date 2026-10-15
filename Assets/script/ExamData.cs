using System.Collections.Generic;
using UnityEngine;

namespace FlamexStudios.Exam
{
    public enum QuestionType
    {
        AudioSpelling,   // Used for Class 1 (50 Words)
        MultipleChoice   // Used for Classes 2-7 (Auto-generates 4 MCQ options!)
    }

    [System.Serializable]
    public class Question
    {
        public QuestionType type = QuestionType.MultipleChoice;

        [Header("1. What Appears in the Library Book")]
        [TextArea(2, 4)]
        [Tooltip("Example: 'Mitochondria is the powerhouse of the cell.' or 'Word: Photosynthesis'")]
        public string bookStudyNote;

        [Header("2. What Appears on the Exam Tablet")]
        [TextArea(2, 3)]
        public string questionText;

        [Header("3. The Single Correct Answer")]
        [Tooltip("Used for BOTH Spelling Bee checking AND auto-building MCQ choices!")]
        public string correctTextAnswer;

        [Header("Audio (Only for Class 1 Spelling Bee)")]
        public AudioClip spokenAudio;

        // Auto-populated at runtime by ExamUIManager (No need to fill in Inspector!)
        [HideInInspector] public string[] runtimeMcqOptions = new string[4];
        [HideInInspector] public int runtimeCorrectButtonIndex;
    }

    [CreateAssetMenu(fileName = "NewExamData", menuName = "FlamexStudios/Exam Data")]
    public class ExamData : ScriptableObject
    {
        public int classNumber = 1;
        public string examTitle = "Class 1: Spelling Bee";
        public List<Question> questions = new List<Question>();
    }
}