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

    // Network sync fields
    private Vector3 networkVelocity = Vector3.zero;
    private Vector3 networkPosition = Vector3.zero;
    private Vector3 estimatedPosition = Vector3.zero;
    // private float lastUpdateTime = 0f;
    // private float positionErrorThreshold = 0.5f;

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

        if (IsOwner)
        {
            CharacterMover();

            // SendPositionToServerRpc(rb.position, rb.linearVelocity); // Gotta add
        }
        else
        {
            // ExtrapolateMovementFromPreviousData(); //Also gotta add
        }


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

    // private void ExtrapolateMovementFromPreviousData()
    // {
    //     estimatedPosition = networkPosition + networkVelocity * ((float)NetworkManager.Singleton.ServerTime.TimeAsFloat - lastUpdateTime);

    //     Vector3 positionError = estimatedPosition - rb.position;
    //     if (positionError.magnitude > positionErrorThreshold)
    //     {
    //         rb.position = Vector3.Lerp(rb.position, estimatedPosition, Time.deltaTime * 10);
    //     }

    //     rb.transform.forward = Vector3.Lerp(rb.transform.forward, networkVelocity.normalized, Time.deltaTime * 10);

    //     rb.linearVelocity = networkVelocity;
    // }


    

    // [ServerRpc]

    // private void SendPositionToServerRpc(Vector3 position, Vector3 velocity)
    // {
    //     networkPosition = position;
    //     networkVelocity = velocity;

    //     SendPositionFromServerToClientRpc(position, velocity, (float)NetworkManager.Singleton.ServerTime.TimeAsFloat);
    // }

    


    // [ClientRpc(Delivery = RpcDelivery.Unreliable)]

    // public void SendPositionFromServerToClientRpc(Vector3 position, Vector3 velocity, float serverTime)
    // {
    //     if (!IsOwner)
    //     {
    //         networkPosition = position;
    //         networkVelocity = velocity;

    //         lastUpdateTime = serverTime;

    //         // Update MovementDirection and ensure y is always 0
    //         Vector3 horizontalVelocity = velocity;
    //         horizontalVelocity.y = 0f;
    //         MovementDirection = horizontalVelocity.sqrMagnitude > 0.001f ? horizontalVelocity.normalized : Vector3.zero;
    //         MovementDirection.y = 0f; // Explicitly set y to 0

    //         MovementSpeed = horizontalVelocity.magnitude;

    //         // Optionally, you can also update the animator or other components here
    //         // animator.SetBool("IsMoving", velocity.magnitude > 0.1f);
    //     }
    // }
}
