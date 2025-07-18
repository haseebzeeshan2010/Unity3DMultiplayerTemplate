using UnityEngine;

public class TagCounter : MonoBehaviour
{
    private Player player;
    [SerializeField, Tooltip("Total time the player has been tagged, in seconds")]
    private float totalTaggedTime = 0f;

    // Timestamp when tagging started, negative if not currently tagged
    private float tagStartTime = -1f;

    // Read-only accessor for other systems
    public float TotalTaggedTime => totalTaggedTime;

    // Subscribe to tag status changes
    void Start()
    {
        player = GetComponent<Player>();
        if (player != null)
        {
            player.TagStatus.OnValueChanged += OnTagStatusChanged;
        }
    }

    // Cleanup event subscription
    void OnDestroy()
    {
        if (player != null)
        {
            player.TagStatus.OnValueChanged -= OnTagStatusChanged;
        }
    }

    // Handle tag status transitions to accumulate tagged duration
    private void OnTagStatusChanged(Player.TagState previous, Player.TagState current)
    {
        if (current == Player.TagState.Tagged)
        {
            // Begin timing when tagged
            tagStartTime = Time.time;
        }
        else if (previous == Player.TagState.Tagged && tagStartTime >= 0f)
        {
            // Accumulate duration and reset
            totalTaggedTime += Time.time - tagStartTime;
            tagStartTime = -1f;
        }
    }
}
