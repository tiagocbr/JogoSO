using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GatoSecundario : MonoBehaviour
{
    public Rigidbody2D rb;

    public float forcaPulo;
    public LayerMask layerChao;
    public float distanciaMinimaChao;
    private bool estaNoChao;
    public Animator animatorComponent;

    private Thread threadSecundaria;
    private bool devePular = false;
    private bool deveAbaixar = false;
    private bool rodando = true; // Controle da thread

    public float distanciaDeteccao = 2f; // Distância máxima para detectar obstáculos

    private List<Vector2> posicoesObstaculos = new List<Vector2>(); // Lista compartilhada de posições
    private readonly object lockObj = new object(); // Objeto de sincronização

    private Vector2 posicaoGato; // Posição do gato atualizada no thread principal

    public float distY = 0;

    public Text finalText;

    public float distXignore = -2;


    void Start()
    {

        // Inicia a thread que calcula os movimentos do gato secundário
        threadSecundaria = new Thread(CalcularMovimentos);
        threadSecundaria.Start();
    }

    void Update()
    {
        posicaoGato = transform.position;
        // Atualiza a lista de posições dos obstáculos no thread principal
        GameObject[] obstaculos = GameObject.FindGameObjectsWithTag("Inimigo");

        lock (lockObj)
        {
            posicoesObstaculos.Clear(); // Limpa a lista para evitar duplicações

            foreach (var obstaculo in obstaculos)
            {
                if (obstaculo != null)
                {
                    posicoesObstaculos.Add(obstaculo.transform.position); // Adiciona as posições
                }
            }
        }

        // Executa as ações calculadas pela thread
        if (devePular)
        {
            Pular();
            devePular = false;
        }

        if (deveAbaixar)
        {
            Abaixar();
        }
        else
        {
            Levantar();
        }
    }

    void Pular()
    {
        if (estaNoChao)
        {
            rb.AddForce(Vector2.up * forcaPulo);
        }
    }

    void Abaixar()
    {
        animatorComponent.SetBool("Abaixado", true);
    }

    void Levantar()
    {
        animatorComponent.SetBool("Abaixado", false);
    }

    private void FixedUpdate()
    {
        // Verifica se está no chão
        estaNoChao = Physics2D.Raycast(transform.position, Vector2.down, distanciaMinimaChao, layerChao);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // Debug.Log(gameObject.name);
        if (other.gameObject.CompareTag("Inimigo"))
        {
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Para a thread ao destruir o objeto
        rodando = false;
        threadSecundaria.Join();
    }

    void CalcularMovimentos()
    {
        // Lógica da thread separada para calcular os movimentos
        while (rodando)
        {
            List<Vector2> obstaculos;
            Vector2 posicaoAtualGato;

            // Copia a lista de posições e a posição do gato para evitar problemas de concorrência
            lock (lockObj)
            {
                obstaculos = new List<Vector2>(posicoesObstaculos);
                posicaoAtualGato = posicaoGato;
            }

            Vector2? obstaculoMaisProximo = null;
            float menorDistanciaX = float.MaxValue;

            // Encontra o obstáculo mais próximo à frente
            foreach (var posicaoObstaculo in obstaculos)
            {
                float distanciaX = posicaoObstaculo.x - posicaoAtualGato.x;

                // Verifica se o obstáculo está à frente (distanciaX > 0)
                if (distanciaX > distXignore && distanciaX < menorDistanciaX)
                {
                    menorDistanciaX = distanciaX;
                    obstaculoMaisProximo = posicaoObstaculo;
                }
            }

            // Aplica a lógica ao obstáculo mais próximo
            if (obstaculoMaisProximo.HasValue && estaNoChao)
            {
                float distanciaY = obstaculoMaisProximo.Value.y - posicaoAtualGato.y;
                float distanciaX = obstaculoMaisProximo.Value.x - posicaoAtualGato.x;

                float distanciaDeteccaoFinal = distanciaDeteccao;
                if (distanciaY > distY)
                { //o obstaculo é um cachorro
                    distanciaDeteccaoFinal += 1;
                }

                // Debug.Log(distanciaY);
                if (distanciaX < distanciaDeteccaoFinal && deveAbaixar == false)
                {
                    if (distanciaY > distY) // Obstáculo está no ar
                    {
                        // Criar uma instância de Random
                        System.Random random = new System.Random();

                        // Gerar um número aleatório entre 1 e 100
                        int numeroAleatorio = random.Next(1, 101);
                        Debug.Log($"aleatorio:{numeroAleatorio}");
                        if (numeroAleatorio <= 1)
                        {
                            Thread.Sleep(2000);
                            return;
                        }
                        devePular = false;
                        deveAbaixar = true;
                    }
                    else // Obstáculo está no chão
                    {
                        if (distanciaX > 0.5)
                        {
                            // Criar uma instância de Random
                            System.Random random = new System.Random();

                            // Gerar um número aleatório entre 1 e 100
                            int numeroAleatorio = random.Next(1, 101);
                            Debug.Log($"aleatorio:{numeroAleatorio}");
                            if (numeroAleatorio <= 1)
                            {
                                Thread.Sleep(2000);
                                return;
                            }
                            deveAbaixar = false;
                            devePular = true;
                        }
                    }
                }
                else
                {
                    if (distanciaX >= distanciaDeteccaoFinal)
                    {
                        deveAbaixar = false;
                        devePular = false;
                    }
                }
            }
            else
            {
                // Se nenhum obstáculo relevante está à frente, redefine as ações
                deveAbaixar = false;
                devePular = false;
            }

            Thread.Sleep(50); // Pausa para evitar sobrecarga (50ms)
        }
    }

}
