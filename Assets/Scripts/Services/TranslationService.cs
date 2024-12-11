using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Models;

namespace Services
{
    public class TranslationService
    {
        private readonly HttpClient _client = new HttpClient();

        public async Task<string> TranslateTextAsync(string text, string fromLang = "en", string toLang = "pt")
        {
            try
            {
                string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={fromLang}|{toLang}";
                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                TranslationResponse translationResponse = JsonUtility.FromJson<TranslationResponse>(responseBody);

                if (translationResponse != null && translationResponse.responseData != null)
                {
                    return translationResponse.responseData.translatedText;
                }
                else
                {
                    Debug.LogError("Erro ao desserializar a tradução.");
                    return null;
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError("Erro na requisição de tradução: " + e.Message);
                return "Erro na tradução.";
            }
            catch (Exception ex)
            {
                Debug.LogError("Erro inesperado na tradução: " + ex.Message);
                return "Erro na tradução.";
            }
        }
    }
}
