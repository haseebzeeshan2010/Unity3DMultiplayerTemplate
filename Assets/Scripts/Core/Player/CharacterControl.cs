using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
public class CharacterControl : MonoBehaviour
{
    [SerializeField] private AnticipatedNetworkTransform networkTransform;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    private Vector3 horizontalVelocity;

    private CharacterController characterController;
    private Vector3 velocity;
    private Vector2 latestMoveInput;

    private NetworkObject networkObject;


    void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component missing on " + gameObject.name);
        }
        if (inputReader != null)
        {
            inputReader.MoveEvent += OnMoveInput;
        }
    }

    void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.MoveEvent -= OnMoveInput;
        }
    }

    private void OnMoveInput(Vector2 input)
    {
        latestMoveInput = input;
    }

    void Update()
    {
        HandleMovement();
    }



    private void HandleMovement()
    {
        Vector2 input = latestMoveInput;
        Vector3 inputDirection = (transform.right * input.x + transform.forward * input.y).normalized;
        Vector3 targetVelocity = inputDirection * moveSpeed;

        // Accelerate or decelerate towards target velocity
        float accelRate = (inputDirection.magnitude > 0) ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accelRate * Time.deltaTime);

        // Move horizontally
        characterController.Move(horizontalVelocity * Time.deltaTime);

        // Gravity and vertical movement
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);

        networkTransform.AnticipateMove(characterController.transform.position);
    }
}