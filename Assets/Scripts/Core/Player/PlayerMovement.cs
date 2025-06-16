using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 4f;
    [SerializeField] private float smoothTime = 0.1f; // Adjustable smoothing factor

    private Vector2 previousMovementInput;
    private Vector3 smoothVelocity; // Used by SmoothDamp

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
        inputReader.MoveEvent += HandleMove;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }
        inputReader.MoveEvent -= HandleMove;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }
        
        Vector3 movementDirection = new Vector3(previousMovementInput.x, 0f, previousMovementInput.y).normalized;
        Vector3 targetPosition = rb.position + movementDirection * movementSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = Vector3.SmoothDamp(rb.position, targetPosition, ref smoothVelocity, smoothTime);
        rb.MovePosition(newPosition);
    }

    private void HandleMove(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }
}
