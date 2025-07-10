using UnityEngine;

/// <summary>
/// Handles smoothly rotating the player character to face their movement direction.
/// Intended for use in both local and networked player contexts.
/// </summary>
public class DirectionalTurn : MonoBehaviour
{
    // Minimum speed to update rotation (to avoid jitter at very low speeds)
    public float minSpeed = 0.1f;

    [SerializeField] private PlayerMovement playerMovement;

    void Update()
    {
        if (playerMovement.MovementDirection.sqrMagnitude > minSpeed * minSpeed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(playerMovement.MovementDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

}
