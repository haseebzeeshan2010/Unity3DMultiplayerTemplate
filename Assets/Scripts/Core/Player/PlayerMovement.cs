using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    //Serialized Fields used to reference the input reader, the body transform, and the rigidbody
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb; // Changed to 3D Rigidbody

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 4f;

    // [SerializeField] private float turningRate = 30f; // Removed turning rate

    private Vector2 previousMovementInput;

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

    // void Update()
    // {
    //     if (!IsOwner) { return; }
    // float zRotation = previousMovementInput.x * -turningRate * Time.deltaTime;
    // bodyTransform.Rotate(0f, 0f, zRotation);
    // }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }

        Vector3 movementDirection = new Vector3(previousMovementInput.x, 0f, previousMovementInput.y).normalized;
        Vector3 targetPosition = rb.position + movementDirection * movementSpeed * Time.fixedDeltaTime;
        rb.MovePosition(Vector3.Lerp(rb.position, targetPosition, 0.1f)); // Adjust the interpolation factor (0.1f) as needed
    }

    private void HandleMove(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }
}
