using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
public class TriviaManager : MonoBehaviour
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

    [Serializable]
    public class TriviaApiResponse
    {
        public int response_code;
        public TriviaQuestion[] results;
    }

    [Serializable]
    public class ResponseData
    {
        public string translatedText; // Campo de interesse
    }

    [Serializable]
    public class TranslationResponse
    {
        public ResponseData responseData; // Objeto contendo a tradução
    }


    private Thread triviaThread;

    private Thread translateThread;
    private readonly Queue<TriviaQuestion> questionQueue = new Queue<TriviaQuestion>();
    private readonly SemaphoreSlim queueSemaphore = new SemaphoreSlim(0); // Para sincronizar quando há perguntas disponíveis

    private readonly SemaphoreSlim rc = new SemaphoreSlim(1); // Para sincronizar quando há perguntas disponíveis

    private readonly SemaphoreSlim queueCapacitySemaphore = new SemaphoreSlim(5, 5); // Limita a fila a no máximo 5 elementos
    private bool isRunning = true;

    public Text questionsText;
    public Text finalText;
    public GameObject jogador2;

    private bool perdi = false;
    public Text pontosText;

    private float pontos;
    private float tempoDecorrido;

    private int qtdPerguntas = 0;
    private const string TriviaApiUrl = "https://opentdb.com/api.php?amount=1&category=18";

    private List<string> currentAnswers;
    private string correctAnswer;

    private Thread translationThread;
    private readonly Queue<TriviaQuestion> translatedQuestionQueue = new Queue<TriviaQuestion>(); // Fila para armazenar perguntas traduzidas
    private readonly object queueLock = new object(); // Para sincronização


    private readonly object queueLock2 = new object(); // Para sincronização
    private bool isRunning2 = true;

    public string textToTranslate = "Hello, how are you today?";
    public string translatedText;

    private void Start()
    {
        triviaThread = new Thread(FetchTriviaQuestions);
        triviaThread.Start();

        translationThread = new Thread(RunTranslation);
        translationThread.Start();

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && perdi)
        {
            Time.timeScale = 1;
            perdi = false;
            SceneManager.LoadScene(0);
            return;
        }

        pontos += Time.deltaTime;
        pontosText.text = Mathf.FloorToInt(pontos).ToString();
        tempoDecorrido += Time.deltaTime;

        int tempoInteiro = Mathf.FloorToInt(tempoDecorrido);
        if ((tempoInteiro / 10) - qtdPerguntas > 0)
        {
            qtdPerguntas++;
            if (questionsText.text != "Parabéns! Você acertou!\n" && questionsText.text != "Ops! Você errou!\n" && questionsText.text != "")
            {
                pontos -= 50;
                pontosText.text = Mathf.FloorToInt(pontos).ToString();
                if (pontos < 0)
                {
                    HandleGameOver();
                }
            }
            var question = ConsumeQuestion();
            if (question != null)
            {
                DisplayQuestion(question);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) ProcessAnswer(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ProcessAnswer(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ProcessAnswer(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ProcessAnswer(3);
    }

    private void HandleGameOver()
    {
        if (jogador2.activeSelf)
        {
            finalText.text = "Você Perdeu! Digite S para reiniciar o jogo.";
            perdi = true;
            Time.timeScale = 0;
        }
        else
        {
            finalText.text = $"Você Sobreviveu mais que o outro gato e ganhou!\nSua pontuação foi de {Mathf.FloorToInt(pontos)}\nDigite S para reiniciar o jogo.";
            perdi = true;
            Time.timeScale = 0;
        }
    }

    private void DisplayQuestion(TriviaQuestion question)
    {
        string formattedText = $"{question.question}\n";
        currentAnswers = new List<string>(question.incorrect_answers);
        correctAnswer = question.correct_answer;
        currentAnswers.Add(correctAnswer);

        // System.Random random = new System.Random();
        // currentAnswers = currentAnswers.OrderBy(x => random.Next()).ToList();

        char optionLetter = '1';
        foreach (string answer in currentAnswers)
        {
            formattedText += $"{optionLetter}) {answer}\n";
            optionLetter++;
        }

        questionsText.text = formattedText;
        Debug.Log($"Pergunta: {question.question}");
        Debug.Log($"Texto formatado: {formattedText}");
    }

    private void ProcessAnswer(int selectedIndex)
    {
        if (currentAnswers == null || selectedIndex >= currentAnswers.Count) return;
        if (questionsText.text == "Parabéns! Você acertou!\n" || questionsText.text == "Ops! Você errou!\n") return;

        string selectedAnswer = currentAnswers[selectedIndex];

        if (selectedAnswer == correctAnswer)
        {
            questionsText.text = "Parabéns! Você acertou!\n";
            pontos += 25;
            pontosText.text = Mathf.FloorToInt(pontos).ToString();
        }
        else
        {
            questionsText.text = "Ops! Você errou!\n";
            pontos -= 50;
            pontosText.text = Mathf.FloorToInt(pontos).ToString();
            if (pontos < 0) HandleGameOver();
        }
    }

    private void OnDestroy()
    {
        // Sinaliza para as threads que elas devem parar
        isRunning = false;
        isRunning2 = false;

        // Aguarda a conclusão das threads, se existirem
        if (triviaThread != null && triviaThread.IsAlive)
        {
            triviaThread.Join();
        }

        if (translationThread != null && translationThread.IsAlive)
        {
            translationThread.Join();
        }

        Debug.Log("Threads encerradas com segurança.");
    }

    private void FetchTriviaQuestions()
    {
        using (HttpClient client = new HttpClient())
        {
            while (isRunning)
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(TriviaApiUrl).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = response.Content.ReadAsStringAsync().Result;
                        TriviaApiResponse apiResponse = JsonUtility.FromJson<TriviaApiResponse>(jsonResponse);

                        if (apiResponse != null && apiResponse.results != null && apiResponse.results.Length > 0)
                        {
                            TriviaQuestion rawQuestion = apiResponse.results[0];
                            TriviaQuestion decodedQuestion = new TriviaQuestion
                            {
                                category = WebUtility.HtmlDecode(rawQuestion.category),
                                type = rawQuestion.type,
                                difficulty = rawQuestion.difficulty,
                                question = WebUtility.HtmlDecode(rawQuestion.question),
                                correct_answer = WebUtility.HtmlDecode(rawQuestion.correct_answer),
                                incorrect_answers = Array.ConvertAll(rawQuestion.incorrect_answers, WebUtility.HtmlDecode)
                            };

                            AddQuestion(decodedQuestion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Erro na thread de perguntas: " + ex.Message);
                }

                Thread.Sleep(1000); // Evita sobrecarregar a API
            }
        }
    }

    private void AddQuestion(TriviaQuestion question)
    {
        queueCapacitySemaphore.Wait();
        rc.Wait();

        questionQueue.Enqueue(question);
        Debug.Log("Pergunta adicionada à fila.");

        rc.Release();
        queueSemaphore.Release();
        Debug.Log("Pergunta adicionada à fila.");
    }

    public TriviaQuestion ConsumeQuestion()
    {
        lock (queueLock2) // Garante que o acesso à fila é thread-safe
        {
            if (translatedQuestionQueue.Count > 0) // Verifica se há elementos na fila
            {
                TriviaQuestion question = translatedQuestionQueue.Dequeue();
                Debug.Log("Pergunta consumida.");
                return question;
            }
            else
            {
                Debug.LogWarning("A fila de perguntas traduzidas está vazia.");
                return null; // Retorna null se a fila estiver vazia
            }
        }
    }

    private async void RunTranslation()
    {
        while (isRunning2)
        {
            TriviaQuestion questionToTranslate = null;

            // Obtém a pergunta da fila `questionQueue`
            queueSemaphore.Wait();
            rc.Wait();

            questionToTranslate = questionQueue.Dequeue();
            Debug.Log("Pergunta consumida.");
            queueCapacitySemaphore.Release(); // Libera um slot no semáforo de capacidade

            rc.Release();



            if (questionToTranslate != null)
            {
                try
                {
                    // Tradução dos membros da pergunta usando TranslateTextAsync
                    questionToTranslate.question = await TranslateTextAsync(questionToTranslate.question);
                    questionToTranslate.correct_answer = await TranslateTextAsync(questionToTranslate.correct_answer);
                    questionToTranslate.incorrect_answers = await Task.WhenAll(
                        questionToTranslate.incorrect_answers.Select(TranslateTextAsync)
                    );

                    // Adiciona à fila de perguntas traduzidas
                    lock (queueLock2)
                    {
                        translatedQuestionQueue.Enqueue(questionToTranslate);
                        Debug.Log($"Pergunta traduzida: {questionToTranslate.question}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Erro ao traduzir pergunta: {ex.Message}");
                }
            }
            else
            {
                // Se não houver perguntas, faz uma pausa para evitar sobrecarga da CPU
                await Task.Delay(500);
            }
        }
    }


    private async Task<string> TranslateTextAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {

                // Envia a requisição GET para a API
                HttpResponseMessage response = await client.GetAsync($"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(url)}&langpair=en|pt");

                // Verifica se a resposta foi bem-sucedida
                response.EnsureSuccessStatusCode();

                // Lê o conteúdo da resposta como string
                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log($"responseBody: {responseBody}");

                TranslationResponse translationResponse = JsonUtility.FromJson<TranslationResponse>(responseBody);

                // Acessa o texto traduzido
                if (translationResponse != null && translationResponse.responseData != null)
                {
                    string textoTraduzido = translationResponse.responseData.translatedText;

                    Debug.LogError($"Tradução : {url} => {textoTraduzido}");
                    return textoTraduzido;
                }
                else
                {
                    Debug.LogError("Erro ao desserializar o JSON ou campo inexistente.");
                    return null;
                }

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Erro ao realizar a requisição:");
                Console.WriteLine(e.Message);
                return "Erro na tradução.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro inesperado:");
                Console.WriteLine(ex.Message);
                return "Erro na tradução.";
            }
        }
    }

}
