using System;

namespace Models
{
    [Serializable]
    public class TriviaQuestion
    {
        public string category;
        public string type;
        public string difficulty;
        public string question;
        public string correct_answer;
        public string[] incorrect_answers;
    }
}
