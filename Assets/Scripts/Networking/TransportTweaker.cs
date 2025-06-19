using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
public class TransportTweaker : MonoBehaviour
{
    [SerializeField] public UnityTransport transport;
    void Start()
    {
        
        if (transport != null)
        {
            // Example: increase receive queue capacity
            // transport.SetConnectionData("127.0.0.1", 7777); // Not required if already set by default
            transport.MaxPacketQueueSize = 512; // adjust as needed


            transport.MaxPayloadSize = 1400; // Increase if your network supports larger MTU
            transport.MaxSendQueueSize = 1024; // Allow more packets to be queued for sending
            // transport.ReceiveQueueCapacity = 1024; // Allow more packets to be queued for receiving (property does not exist)
            // transport.ReceiveQueueBatchSize = 128; // Increase batch size for faster processing (property does not exist)
            
        }
        else
        {
            Debug.LogWarning("UnityTransport component not found on NetworkManager.");
        }
    }
}