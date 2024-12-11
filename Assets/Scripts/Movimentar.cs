using UnityEngine;

public class Movimentar : MonoBehaviour
{

    public Vector2 direcao;

    public float velocidade;

    // Update is called once per frame
    private void Update()
    {
        transform.Translate(direcao * velocidade * Time.deltaTime);
    }
}
