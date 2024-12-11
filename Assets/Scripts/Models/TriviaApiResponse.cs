using System;

namespace Models
{
    [Serializable]
    public class TriviaApiResponse
    {
        public int response_code;
        public TriviaQuestion[] results;
    }
}
