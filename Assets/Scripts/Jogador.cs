using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Jogador : MonoBehaviour
{

    public Rigidbody2D rb;

    public float forcaPulo;

    public LayerMask layerChao;

    public float distanciaMinimaChao;

    private bool estaNoChao;

    public Text finalText;


    public Animator animatorComponent;

    public GameObject jogador2;

    private bool perdi = false;


    void Update()
    {

        if (Input.GetKeyDown(KeyCode.S) && perdi)
        {
            Time.timeScale = 1;
            perdi = false;
            SceneManager.LoadScene(0);
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Pular();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Abaixar();
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow))
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
        estaNoChao = Physics2D.Raycast(transform.position, Vector2.down, distanciaMinimaChao, layerChao);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Inimigo"))
        {
            if (jogador2.activeSelf)
            {
                finalText.text = "Você Perdeu! Digite S para reiniciar o jogo.";
                perdi = true;
                Time.timeScale = 0;
            }
            else
            {
                finalText.text = $"Você Sobreviveu mais que o outro gato e ganhou!\nDigite S para reiniciar o jogo.";
                perdi = true;
                Time.timeScale = 0;
            }
        }
    }
}
