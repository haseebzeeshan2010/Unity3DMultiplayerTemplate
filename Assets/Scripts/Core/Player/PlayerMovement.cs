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
    [SerializeField] private float smoothTime = 0.1f; // Adjustable smoothing factor

    private Vector2 previousMovementInput;
    private Vector3 smoothVelocity; // Used by SmoothDamp

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

        Vector3 currentPos = rb.position;

        Vector3 movementDirection = new Vector3(previousMovementInput.x, 0f, previousMovementInput.y).normalized;
        Vector3 targetPosition = rb.position + movementDirection * movementSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = Vector3.SmoothDamp(rb.position, targetPosition, ref smoothVelocity, smoothTime);

        rb.MovePosition(newPosition);

        // Determine the actual movement direction after MovePosition
        MovementDirection = (rb.position - currentPos).normalized;

        MovementSpeed = (newPosition - currentPos).magnitude / Time.fixedDeltaTime;

        if (MovementSpeed > 2f)
        {
            IsMoving.Value = true;
        }
        else
        {
            IsMoving.Value = false;
        }
        
        
    }

    private void HandleMove(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }
}
