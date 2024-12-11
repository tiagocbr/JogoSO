using UnityEngine;

public class Chao : MonoBehaviour
{
    public float diferencaX;
    public float iniX;

    private void Update()
    {
        if (transform.position.x <= iniX)
        {
            transform.position = new Vector3(transform.position.x + diferencaX * 2,
            transform.position.y, transform.position.z);
        }
    }
}
