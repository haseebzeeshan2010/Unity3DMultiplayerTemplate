using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 4f;
    [SerializeField, Tooltip("How quickly the player accelerates/decelerates.")]
    private float accelerationSmoothTime = 0.1f; // Adjustable smoothing factor

    private Vector2 previousMovementInput;
    private Vector3 currentVelocity; // Used by SmoothDamp

    public Vector3 MovementDirection;
    public float MovementSpeed;

    public NetworkVariable<bool> IsMoving = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
        inputReader.MoveEvent += HandleMove;
        IsMoving.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }
        inputReader.MoveEvent -= HandleMove;
    }

    private void FixedUpdate()
    {
        animator.SetBool("IsMoving", IsMoving.Value);

        if (!IsOwner) { return; }

        CharacterMover();
    }

    private void HandleMove(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }

    private void CharacterMover()
    {
        Vector3 movementDirection = new Vector3(previousMovementInput.x, 0f, previousMovementInput.y).normalized;

        // Calculate desired velocity
        Vector3 desiredVelocity = movementDirection * movementSpeed;

        // Smoothly interpolate velocity
        Vector3 smoothedVelocity = Vector3.SmoothDamp(
            rb.linearVelocity,
            desiredVelocity,
            ref currentVelocity,
            accelerationSmoothTime
        );

        Vector3 velocityChange = smoothedVelocity - rb.linearVelocity;
        velocityChange.y = 0f; // Prevent vertical force

        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        // Update movement direction and speed for animation/network
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;
        MovementDirection = horizontalVelocity.sqrMagnitude > 0.001f ? horizontalVelocity.normalized : Vector3.zero;
        MovementSpeed = horizontalVelocity.magnitude;

        IsMoving.Value = MovementSpeed > 3f;
    }
}
