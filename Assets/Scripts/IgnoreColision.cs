using UnityEngine;

public class DynamicIgnoreCollision : MonoBehaviour
{
    public GameObject child1Object1; // Primeiro filho do Objeto 1
    public GameObject child2Object1; // Segundo filho do Objeto 1
    public GameObject child1Object2; // Primeiro filho do Objeto 2
    public GameObject child2Object2; // Segundo filho do Objeto 2

    private GameObject activeCollider1; // Collider ativo do Objeto 1
    private GameObject activeCollider2; // Collider ativo do Objeto 2

    void Start()
    {
        // Inicializa os colisores ativos
        activeCollider1 = child1Object1.activeSelf ? child1Object1 : child2Object1;
        activeCollider2 = child1Object2.activeSelf ? child1Object2 : child2Object2;

        ApplyIgnoreCollisions();
    }

    void FixedUpdate()
    {
        // Verifica se houve mudança nos filhos ativos
        GameObject newActiveCollider1 = child1Object1.activeSelf ? child1Object1 : child2Object1;
        GameObject newActiveCollider2 = child1Object2.activeSelf ? child1Object2 : child2Object2;

        if (newActiveCollider1 != activeCollider1 || newActiveCollider2 != activeCollider2)
        {
            activeCollider1 = newActiveCollider1;
            activeCollider2 = newActiveCollider2;
            ApplyIgnoreCollisions();
        }
    }

    void ApplyIgnoreCollisions()
    {
        // Obtém os PolygonCollider2D dos colisores ativos
        PolygonCollider2D collider1 = activeCollider1.GetComponent<PolygonCollider2D>();
        PolygonCollider2D collider2 = activeCollider2.GetComponent<PolygonCollider2D>();

        // Verifica se os componentes de colisores existem
        if (collider1 == null || collider2 == null)
        {
            Debug.LogError("Um ou mais colisores ativos não têm PolygonCollider2D!");
            return;
        }

        // Aplica a lógica de ignorar colisões entre os dois filhos ativos
        Physics2D.IgnoreCollision(collider1, collider2);

        // Debug.Log($"Ignorando colisão entre {collider1.name} e {collider2.name}");
    }
}
