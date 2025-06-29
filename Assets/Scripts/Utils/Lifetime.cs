using UnityEngine;
using UnityEngine.Serialization;

namespace Utils
{
    public class Lifetime : MonoBehaviour
    {
        [FormerlySerializedAs("DestroyAfterSeconds")] [SerializeField] public int destroyAfterSeconds = 1;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Destroy(gameObject, destroyAfterSeconds);
        }
    }
}
