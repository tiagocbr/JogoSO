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
    private bool rodando = true;

    public float distanciaDeteccao = 2f;

    private List<Vector2> posicoesObstaculos = new List<Vector2>();
    private readonly object lockObj = new object();

    private Vector2 posicaoGato;

    public float distY = 0;

    public Text finalText;

    public float distXignore = -2;


    void Start()
    {
        threadSecundaria = new Thread(CalcularMovimentos);
        threadSecundaria.Start();
    }

    void Update()
    {
        posicaoGato = transform.position;

        GameObject[] obstaculos = GameObject.FindGameObjectsWithTag("Inimigo");

        lock (lockObj)
        {
            posicoesObstaculos.Clear();
            foreach (var obstaculo in obstaculos) if (obstaculo != null) posicoesObstaculos.Add(obstaculo.transform.position);
        }

        if (devePular)
        {
            Pular();
            devePular = false;
        }

        if (deveAbaixar) Abaixar();
        else Levantar();
    }

    void Pular()
    {
        if (estaNoChao) rb.AddForce(Vector2.up * forcaPulo);
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
        estaNoChao = Physics2D.Raycast(transform.position, Vector2.down, distanciaMinimaChao, layerChao);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Inimigo")) gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        rodando = false;
        threadSecundaria.Join();
    }

    void CalcularMovimentos()
    {
        while (rodando)
        {
            List<Vector2> obstaculos;
            Vector2 posicaoAtualGato;

            lock (lockObj)
            {
                obstaculos = new List<Vector2>(posicoesObstaculos);
                posicaoAtualGato = posicaoGato;
            }

            Vector2? obstaculoMaisProximo = null;
            float menorDistanciaX = float.MaxValue;

            foreach (var posicaoObstaculo in obstaculos)
            {
                float distanciaX = posicaoObstaculo.x - posicaoAtualGato.x;

                if (distanciaX > distXignore && distanciaX < menorDistanciaX)
                {
                    menorDistanciaX = distanciaX;
                    obstaculoMaisProximo = posicaoObstaculo;
                }
            }

            if (obstaculoMaisProximo.HasValue && estaNoChao)
            {
                float distanciaY = obstaculoMaisProximo.Value.y - posicaoAtualGato.y;
                float distanciaX = obstaculoMaisProximo.Value.x - posicaoAtualGato.x;

                float distanciaDeteccaoFinal = distanciaDeteccao;
                if (distanciaY > distY) distanciaDeteccaoFinal += 1;

                if (distanciaX < distanciaDeteccaoFinal && deveAbaixar == false)
                {
                    if (distanciaY > distY)
                    {
                        System.Random random = new System.Random();

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
                    else
                    {
                        if (distanciaX > 0.5)
                        {
                            System.Random random = new System.Random();

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
                deveAbaixar = false;
                devePular = false;
            }

            Thread.Sleep(50);
        }
    }

}
