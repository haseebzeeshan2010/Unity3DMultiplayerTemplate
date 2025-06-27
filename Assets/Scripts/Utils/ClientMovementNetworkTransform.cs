using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
{
    [DisallowMultipleComponent]
    public class ClientMovementNetworkTransform : NetworkTransform
    {
        private struct State
        {
            public Vector3 Position;
            public double Timestamp;
        }

        [Header("Interpolation Settings")]
        [SerializeField] private float interpolationBackTime = 0.2f;
        [SerializeField] private float smoothingSpeed = 10f;
        [SerializeField] private float snapDistance = 5f;
        [SerializeField] private float extrapolationMultiplier = 1.2f;

        [Header("Buffer Settings")]
        [SerializeField] private int maxBufferSize = 40;
        [SerializeField] private float maxStateAge = 1.0f;
        [SerializeField] private int velocityBufferSize = 5;

        private Queue<State> stateBuffer = new Queue<State>();
        private Queue<Vector3> velocityBuffer = new Queue<Vector3>();
        private Vector3 smoothVelocity;

        protected override void Awake()
        {
            base.Awake();
            stateBuffer.Clear();
            EnqueueState(transform.position, NetworkManager.Singleton.LocalTime.TimeAsFloat);
        }

        protected override void OnInitialize(ref NetworkTransformState state)
        {
            base.OnInitialize(ref state);
            stateBuffer.Clear();
            velocityBuffer.Clear();
            EnqueueState(transform.position, NetworkManager.Singleton.LocalTime.TimeAsFloat);
        }

        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);

            if (IsOwner)
            {
                // Client-side prediction
                double timestamp = NetworkManager.Singleton.LocalTime.Time;
                Vector3 velocity = GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                Vector3 predicted = transform.position + velocity * Time.deltaTime;
                EnqueueState(predicted, timestamp);
            }
            else
            {
                // Reconcile with server state
                double timestamp = NetworkManager.Singleton.LocalTime.Time;
                Vector3 pos = newState.GetPosition(); // correct API :contentReference[oaicite:1]{index=1}
                EnqueueState(pos, timestamp);

                double interpTime = timestamp - interpolationBackTime;
                while (stateBuffer.Count >= 2 && stateBuffer.Peek().Timestamp < interpTime)
                    stateBuffer.Dequeue();
                while (stateBuffer.Count > 0 && timestamp - stateBuffer.Peek().Timestamp > maxStateAge)
                    stateBuffer.Dequeue();

                var arr = stateBuffer.ToArray();
                Vector3 targetPos;
                if (arr.Length >= 4)
                {
                    var p0 = arr[0]; var p1 = arr[1]; var p2 = arr[2]; var p3 = arr[3];
                    float t = Mathf.InverseLerp((float)p1.Timestamp, (float)p2.Timestamp, (float)interpTime);
                    targetPos = CatmullRom(p0.Position, p1.Position, p2.Position, p3.Position, t);
                    UpdateVelocity(p1, p2);
                }
                else if (arr.Length >= 2)
                {
                    var older = arr[0]; var newer = arr[1];
                    float t = Mathf.InverseLerp((float)older.Timestamp, (float)newer.Timestamp, (float)interpTime);
                    targetPos = Vector3.Lerp(older.Position, newer.Position, t);
                    UpdateVelocity(older, newer);
                }
                else if (arr.Length == 1)
                {
                    var last = arr[0];
                    double delta = Mathf.Min(interpolationBackTime, (float)(interpTime - last.Timestamp));
                    targetPos = last.Position + GetSmoothedVelocity() * (float)delta * extrapolationMultiplier;
                }
                else
                {
                    targetPos = transform.position + GetSmoothedVelocity() * Time.deltaTime * extrapolationMultiplier;
                }

                ApplySmoothedPosition(targetPos);
            }
        }

        private void EnqueueState(Vector3 pos, double timestamp)
        {
            stateBuffer.Enqueue(new State { Position = pos, Timestamp = timestamp });
            if (stateBuffer.Count > maxBufferSize)
                stateBuffer.Dequeue();
        }

        private void UpdateVelocity(State a, State b)
        {
            double dt = b.Timestamp - a.Timestamp;
            if (dt <= 0) return;
            Vector3 vel = (b.Position - a.Position) / (float)dt;
            velocityBuffer.Enqueue(vel);
            if (velocityBuffer.Count > velocityBufferSize)
                velocityBuffer.Dequeue();
        }

        private Vector3 GetSmoothedVelocity()
        {
            if (velocityBuffer.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var v in velocityBuffer) sum += v;
            return sum / velocityBuffer.Count;
        }

        private void ApplySmoothedPosition(Vector3 targetPos)
        {
            float sqrDist = (transform.position - targetPos).sqrMagnitude;
            if (sqrDist > snapDistance * snapDistance)
            {
                transform.position = targetPos;
                smoothVelocity = Vector3.zero;
                velocityBuffer.Clear();
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref smoothVelocity, 1f / smoothingSpeed);
            }
        }

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
            );
        }

        protected override bool OnIsServerAuthoritative() => false; // client authority

    }
}




//Code below is for simple smoothing


// using Unity.Netcode.Components;
// using UnityEngine;
// using System.Collections.Generic;

// namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
// {
//     [DisallowMultipleComponent]
//     public class ClientMovementNetworkTransform : NetworkTransform
//     {
//         private struct State
//         {
//             public Vector3 Position;
//             public Quaternion Rotation;
//             public float Timestamp;
//         }

//         private Queue<State> stateBuffer = new Queue<State>();
//         private const float interpolationBackTime = 0.2f; // 200ms buffer for bad networks
//         private const int maxBufferSize = 20;

//         private Vector3 velocity; // For extrapolation

//         public override void OnNetworkSpawn()
//         {
//             base.OnNetworkSpawn();
//             stateBuffer.Clear();
//             stateBuffer.Enqueue(new State { Position = transform.position, Rotation = transform.rotation, Timestamp = Time.time });
//         }

//         private void Update()
//         {
//             if (!IsOwner) // use (!IsOwner && !IsServer) if you want to allow host to use smoothing for remote objects it does not own
//             {
//                 float interpTime = Time.time - interpolationBackTime;

//                 // Remove old states
//                 while (stateBuffer.Count >= 2 && stateBuffer.Peek().Timestamp < interpTime)
//                     stateBuffer.Dequeue();

//                 State[] states = stateBuffer.ToArray();

//                 if (stateBuffer.Count >= 2)
//                 {
//                     State newer = states[1];
//                     State older = states[0];

//                     float t = Mathf.InverseLerp(older.Timestamp, newer.Timestamp, interpTime);

//                     // Interpolate position and rotation
//                     Vector3 targetPos = Vector3.Lerp(older.Position, newer.Position, t);
//                     Quaternion targetRot = Quaternion.Slerp(older.Rotation, newer.Rotation, t);

//                     // Smooth correction
//                     transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
//                     transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.5f);

//                     // Update velocity for extrapolation
//                     velocity = (newer.Position - older.Position) / (newer.Timestamp - older.Timestamp + 0.0001f);
//                 }
//                 else if (stateBuffer.Count == 1)
//                 {
//                     // Extrapolate if possible
//                     State last = states[0];
//                     float delta = interpTime - last.Timestamp;
//                     Vector3 extrapolated = last.Position + velocity * delta;
//                     transform.position = Vector3.Lerp(transform.position, extrapolated, 0.2f);
//                     transform.rotation = Quaternion.Slerp(transform.rotation, last.Rotation, 0.2f);
//                 }
//             }
//         }

//         public void SetAuthoritativeState(Vector3 newPosition, Quaternion newRotation)
//         {
//             stateBuffer.Enqueue(new State { Position = newPosition, Rotation = newRotation, Timestamp = Time.time });
//             while (stateBuffer.Count > maxBufferSize)
//                 stateBuffer.Dequeue();
//         }

//         // For backwards compatibility if you only want to set position
//         public void SetAuthoritativeState(Vector3 newPosition)
//         {
//             SetAuthoritativeState(newPosition, transform.rotation);
//         }

//         protected override bool OnIsServerAuthoritative()
//         {
//             return false;
//         }
//     }
// }