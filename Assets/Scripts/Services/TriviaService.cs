using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Models;

namespace Services
{
    public class TriviaService
    {
        private const string TriviaApiUrl = "https://opentdb.com/api.php?amount=1&category=18";
        private readonly HttpClient _client = new HttpClient();

        public async Task<TriviaApiResponse> FetchTriviaQuestionsAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync(TriviaApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    TriviaApiResponse apiResponse = JsonUtility.FromJson<TriviaApiResponse>(jsonResponse);
                    return apiResponse;
                }
                else
                {
                    Debug.LogError($"Erro ao buscar perguntas: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Erro na TriviaService: " + ex.Message);
                return null;
            }
        }
    }
}
