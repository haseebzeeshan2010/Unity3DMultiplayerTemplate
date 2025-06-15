using UnityEngine;

public class Lifetime : MonoBehaviour
{
    [SerializeField] public int DestroyAfterSeconds = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, DestroyAfterSeconds);
    }
}
