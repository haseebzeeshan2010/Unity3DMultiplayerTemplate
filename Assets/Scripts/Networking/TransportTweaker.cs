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

            transport.MaxPacketQueueSize = 512; // Reasonable queue size


            transport.MaxPayloadSize = 1200; // 1200 bytes is safer for WebGL (MTU limits)
            transport.MaxSendQueueSize = 512; // Conservative for browser memory limits

            //For Faster Performance on Desktop
            // transport.MaxPacketQueueSize = 512; // adjust as needed


            // transport.MaxPayloadSize = 1400; // Increase if your network supports larger MTU
            // transport.MaxSendQueueSize = 1024; // Allow more packets to be queued for sending

        }
        else
        {
            Debug.LogWarning("UnityTransport component not found on NetworkManager.");
        }
    }
}