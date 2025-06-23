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
            public float Timestamp;
        }

        private Queue<State> stateBuffer = new Queue<State>();
        private const float interpolationBackTime = 0.2f;
        private const int maxBufferSize = 40;
        private const float smoothingSpeed = 20f;
        private const float snapDistance = 5f;

        private Vector3 velocity;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            stateBuffer.Clear();
            stateBuffer.Enqueue(new State { Position = transform.position, Timestamp = Time.time });
        }

        private void Update()
        {
            if (!IsOwner || (IsServer && !IsOwner))
            {
                float interpTime = Time.time - interpolationBackTime;

                // Remove old states
                while (stateBuffer.Count >= 2 && stateBuffer.Peek().Timestamp < interpTime)
                    stateBuffer.Dequeue();

                State[] states = stateBuffer.ToArray();

                if (stateBuffer.Count >= 4)
                {
                    // Catmull-Rom spline interpolation
                    State p0 = states[0];
                    State p1 = states[1];
                    State p2 = states[2];
                    State p3 = states[3];

                    // Find where interpTime fits between p1 and p2
                    float t = Mathf.InverseLerp(p1.Timestamp, p2.Timestamp, interpTime);

                    Vector3 targetPos = CatmullRom(p0.Position, p1.Position, p2.Position, p3.Position, t);

                    if ((transform.position - targetPos).sqrMagnitude > snapDistance * snapDistance)
                    {
                        transform.position = targetPos;
                    }
                    else
                    {
                        float lerpFactor = 1 - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
                        transform.position = Vector3.Lerp(transform.position, targetPos, lerpFactor);
                    }

                    velocity = (p2.Position - p1.Position) / (p2.Timestamp - p1.Timestamp + 0.0001f);
                }
                else if (stateBuffer.Count >= 2)
                {
                    State newer = states[1];
                    State older = states[0];

                    float t = Mathf.InverseLerp(older.Timestamp, newer.Timestamp, interpTime);

                    Vector3 targetPos = Vector3.Lerp(older.Position, newer.Position, t);

                    if ((transform.position - targetPos).sqrMagnitude > snapDistance * snapDistance)
                    {
                        transform.position = targetPos;
                    }
                    else
                    {
                        float lerpFactor = 1 - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
                        transform.position = Vector3.Lerp(transform.position, targetPos, lerpFactor);
                    }

                    velocity = (newer.Position - older.Position) / (newer.Timestamp - older.Timestamp + 0.0001f);
                }
                else if (stateBuffer.Count == 1)
                {
                    State last = states[0];
                    float delta = interpTime - last.Timestamp;
                    Vector3 extrapolated = last.Position + velocity * delta;
                    float lerpFactor = 1 - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
                    transform.position = Vector3.Lerp(transform.position, extrapolated, lerpFactor);
                }
            }
        }

        // Catmull-Rom spline interpolation for Vector3
        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // Standard Catmull-Rom spline formula
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
            );
        }

        public void SetAuthoritativeState(Vector3 newPosition)
        {
            stateBuffer.Enqueue(new State { Position = newPosition, Timestamp = Time.time });
            while (stateBuffer.Count > maxBufferSize)
                stateBuffer.Dequeue();
        }

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
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