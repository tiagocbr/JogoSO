using UnityEngine;

public class DynamicIgnoreCollision : MonoBehaviour
{
    public GameObject child1Object1;
    public GameObject child2Object1;
    public GameObject child1Object2;
    public GameObject child2Object2;

    private GameObject activeCollider1;
    private GameObject activeCollider2;

    void Start()
    {
        activeCollider1 = child1Object1.activeSelf ? child1Object1 : child2Object1;
        activeCollider2 = child1Object2.activeSelf ? child1Object2 : child2Object2;

        ApplyIgnoreCollisions();
    }

    void FixedUpdate()
    {
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
        PolygonCollider2D collider1 = activeCollider1.GetComponent<PolygonCollider2D>();
        PolygonCollider2D collider2 = activeCollider2.GetComponent<PolygonCollider2D>();

        if (collider1 == null || collider2 == null)
        {
            Debug.LogError("Um ou mais colisores ativos não têm PolygonCollider2D!");
            return;
        }

        Physics2D.IgnoreCollision(collider1, collider2);
    }
}
