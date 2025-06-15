using UnityEngine;
using Unity.Netcode;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform turretTransform;

    private void LateUpdate()
    {
        // if (!IsOwner) {return;}

        // // Convert mouse position to world point and calculate direction
        // Vector2 aimWorldPosition = Camera.main.ScreenToWorldPoint(inputReader.AimPosition);
        // Vector2 aimDirection = aimWorldPosition - (Vector2)turretTransform.position;

        // turretTransform.up = aimDirection;
    }
}
