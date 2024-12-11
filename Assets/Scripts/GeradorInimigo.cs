using UnityEngine;

public class GeradorInimigo : MonoBehaviour
{
    public GameObject[] baldePrefab;

    public GameObject cachorroVoadorPrefab;

    public float cachorroVoadorYMaximo;

    public float cachorroVoadorYMinimo;

    public float delayInicial;

    public float delayEntreBaldes;

    private void Start()
    {
        InvokeRepeating("GerarInimigo", delayInicial, delayEntreBaldes);
    }

    private void GerarInimigo()
    {
        var dado = Random.Range(1, 7);

        if (dado <= 2)
        {
            var posicaoYAleatoria = Random.Range(cachorroVoadorYMinimo, cachorroVoadorYMaximo);

            var posicao = new Vector3(
                transform.position.x,
                transform.position.y + posicaoYAleatoria,
                transform.position.z
            );

            Instantiate(cachorroVoadorPrefab, posicao, Quaternion.identity);
        }
        else
        {
            var balde = baldePrefab[Random.Range(0, baldePrefab.Length)];
            Instantiate(balde, transform.position, Quaternion.identity);
        }
    }
}
