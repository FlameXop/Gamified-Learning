using System.Collections.Generic;
using UnityEngine;

namespace FlamexStudios.Exam
{
    public enum QuestionType
    {
        MultipleChoice, // For Math/Physics
        AudioSpelling   // For Class 1 Spelling Bee
    }

    [System.Serializable]
    public class Question
    {
        public QuestionType type;

        [TextArea(3, 5)]
        public string questionText; // e.g., "Listen and type the word:"

        [Header("For Audio / Typed Questions")]
        public AudioClip spokenAudio;     // Drag your Text-to-Speech audio file here
        public string correctTextAnswer;  // e.g., "photosynthesis"

        [Header("For MCQ")]
        public string[] answers = new string[4];
        public int correctAnswerIndex;
    }

    [CreateAssetMenu(fileName = "NewExam", menuName = "FlamexStudios/Exam Data")]
    public class ExamData : ScriptableObject
    {
        public int requiredClassLevel = 1;
        public string examTitle;
        public float timeLimitSeconds = 300f;
        public List<Question> questions = new List<Question>();
    }
}