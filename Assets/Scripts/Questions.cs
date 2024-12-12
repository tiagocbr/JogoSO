using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Models;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Questions : MonoBehaviour
{
    public Text questionsText;
    public Text finalText;
    public GameObject jogador2;
    public Text pontosText;

    private bool perdi = false;
    private float pontos;
    private float tempoDecorrido;
    private int qtdPerguntas = 0;

    private readonly Queue<TriviaQuestion> questionQueue = new Queue<TriviaQuestion>();
    private readonly Queue<TriviaQuestion> translatedQuestionQueue = new Queue<TriviaQuestion>();
    private readonly SemaphoreSlim queueSemaphore = new SemaphoreSlim(0);
    private readonly SemaphoreSlim queueCapacitySemaphore = new SemaphoreSlim(5, 5);

    private readonly SemaphoreSlim rc = new SemaphoreSlim(1);
    private readonly object queueLock2 = new object();

    private TriviaService triviaService;
    private TranslationService translationService;

    private List<string> currentAnswers;
    private string correctAnswer;

    private CancellationTokenSource cancellationTokenSource;

    private void Awake()
    {
        triviaService = new TriviaService();
        translationService = new TranslationService();
        cancellationTokenSource = new CancellationTokenSource();

        Task.Run(() => FetchTriviaQuestionsAsync(cancellationTokenSource.Token));
        Task.Run(() => RunTranslationAsync(cancellationTokenSource.Token));
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

        // Sortear as alternativas
        System.Random random = new System.Random();
        currentAnswers = currentAnswers.OrderBy(x => random.Next()).ToList();

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

    private async Task FetchTriviaQuestionsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                TriviaApiResponse apiResponse = await triviaService.FetchTriviaQuestionsAsync();
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
            catch (Exception ex)
            {
                Debug.LogError("Erro ao buscar perguntas: " + ex.Message);
            }

            await Task.Delay(1000, token);
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
    }

    public TriviaQuestion ConsumeQuestion()
    {
        lock (queueLock2)
        {
            if (translatedQuestionQueue.Count > 0)
            {
                TriviaQuestion question = translatedQuestionQueue.Dequeue();
                Debug.Log("Pergunta consumida.");
                return question;
            }
            else
            {
                Debug.LogWarning("A fila de perguntas traduzidas está vazia.");
                return null;
            }
        }

    }

    private async Task RunTranslationAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TriviaQuestion questionToTranslate = null;



            queueSemaphore.Wait();

            rc.Wait();

            questionToTranslate = questionQueue.Dequeue();

            rc.Release();

            queueCapacitySemaphore.Release();

            if (questionToTranslate != null)
            {
                try
                {
                    questionToTranslate.question = await translationService.TranslateTextAsync(questionToTranslate.question);
                    questionToTranslate.correct_answer = await translationService.TranslateTextAsync(questionToTranslate.correct_answer);

                    for (int i = 0; i < questionToTranslate.incorrect_answers.Length; i++)
                    {
                        questionToTranslate.incorrect_answers[i] = await translationService.TranslateTextAsync(questionToTranslate.incorrect_answers[i]);
                    }

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
                await Task.Delay(500, token);
            }
        }
    }

    private void OnDestroy()
    {
        cancellationTokenSource.Cancel();
    }
}
