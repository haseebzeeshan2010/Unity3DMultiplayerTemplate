using UnityEngine;

public class DirectionalTurn : MonoBehaviour
{
    // Minimum speed to update rotation (to avoid jitter at very low speeds)
    public float minSpeed = 0.1f;

    [SerializeField] private PlayerMovement playerMovement;

    // private Animator animator;
    // private int VelocityHash;
    // private void Start()
    // {
    //     animator = GetComponent<Animator>();

    //     VelocityHash = Animator.StringToHash("Velocity"); // makes it more efficient
    // }
    void Update()
    {
        if (playerMovement.MovementDirection.sqrMagnitude > minSpeed * minSpeed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(playerMovement.MovementDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        Debug.Log("Current Speed: " + playerMovement.MovementSpeed);

        // animator.SetFloat(VelocityHash, playerMovement.MovementSpeed);

    }

}
