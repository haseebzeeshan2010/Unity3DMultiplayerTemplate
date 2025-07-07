/*
ClientMovementNetworkTransform Call Flow Overview:

1. Initialization
    - Awake()
    - OnInitialize()
      → Both clear state/velocity buffers and add the initial transform snapshot.

2. Network State Updates
    - OnNetworkTransformStateUpdated()
      ├─ If IsOwner:
      │     → ProcessOwnerMovement()
      │         → Predicts local movement and records a new snapshot.
      └─ Else:
              → ProcessRemoteMovement()
                    → AddStateSnapshot()         // Add received network state
                    → PruneOldStates()           // Remove outdated snapshots
                    → ComputeTargetPosition()    // Choose target position:
                         ├─ InterpolateUsingCatmullRom() // 4+ states
                         ├─ InterpolateLinearly()        // 2-3 states
                         └─ ExtrapolateFromLastState()   // 1 state
                    → ApplySmoothedPosition()    // Smoothly move towards target

3. Helper Methods
    - AddStateSnapshot()         // Add new position/timestamp to buffer
    - PruneOldStates()           // Remove old states from buffer
    - RecordVelocity()           // Store velocity between states
    - ComputeAverageVelocity()   // Average recent velocities
    - CatmullRom()               // Catmull-Rom spline interpolation
*/

using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Utils
{
    [DisallowMultipleComponent]
    public class ClientMovementNetworkTransform : NetworkTransform
    {
        private struct TransformState
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
        [SerializeField] private float maxStateAge = 1f;
        [SerializeField] private int velocityBufferSize = 5;

        private readonly Queue<TransformState> _stateSnapshots = new Queue<TransformState>();
        private readonly Queue<Vector3> _velocityBuffer  = new Queue<Vector3>();
        private Vector3 _smoothVelocity;

        private double CurrentTime => NetworkManager.Singleton.LocalTime.Time;
        private float DeltaTime   => Time.deltaTime;

        protected override void Awake()
        {
            base.Awake();
            _stateSnapshots.Clear();
            AddStateSnapshot(transform.position, CurrentTime);
        }

        protected override void OnInitialize(ref NetworkTransformState state)
        {
            base.OnInitialize(ref state);
            _stateSnapshots.Clear();
            _velocityBuffer.Clear();
            AddStateSnapshot(transform.position, CurrentTime);
        }

        protected override void OnNetworkTransformStateUpdated(
            ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
            if (IsOwner) ProcessOwnerMovement();
            else        ProcessRemoteMovement(newState);
        }

        private void ProcessOwnerMovement()
        {
            var rb       = GetComponent<Rigidbody>();
            Vector3 vel  = rb != null ? rb.linearVelocity : Vector3.zero;
            Vector3 pred = transform.position + vel * DeltaTime;
            AddStateSnapshot(pred, CurrentTime);
        }

        private void ProcessRemoteMovement(NetworkTransformState state)
        {
            double interpTime = CurrentTime - interpolationBackTime;
            AddStateSnapshot(state.GetPosition(), CurrentTime);
            PruneOldStates(interpTime);

            Vector3 target = ComputeTargetPosition(interpTime);
            ApplySmoothedPosition(target);
        }

        private void PruneOldStates(double cutoffTime)
        {
            while (_stateSnapshots.Count >= 2 && _stateSnapshots.Peek().Timestamp < cutoffTime)
                _stateSnapshots.Dequeue();

            while (_stateSnapshots.Count > 0 &&
                   CurrentTime - _stateSnapshots.Peek().Timestamp > maxStateAge)
                _stateSnapshots.Dequeue();
        }

        private Vector3 ComputeTargetPosition(double interpTime)
        {
            var states = _stateSnapshots.ToArray();
            if (states.Length >= 4) return InterpolateUsingCatmullRom(states, interpTime);
            if (states.Length >= 2) return InterpolateLinearly(states, interpTime);
            if (states.Length == 1) return ExtrapolateFromLastState(states[0], interpTime);

            return transform.position
                 + ComputeAverageVelocity() * DeltaTime * extrapolationMultiplier;
        }

        private Vector3 InterpolateUsingCatmullRom(TransformState[] s, double t)
        {
            float u = Mathf.InverseLerp((float)s[1].Timestamp, (float)s[2].Timestamp, (float)t);
            RecordVelocity(s[1], s[2]);
            return CatmullRom(
                s[0].Position, s[1].Position, s[2].Position, s[3].Position, u
            );
        }

        private Vector3 InterpolateLinearly(TransformState[] s, double t)
        {
            float u = Mathf.InverseLerp((float)s[0].Timestamp, (float)s[1].Timestamp, (float)t);
            RecordVelocity(s[0], s[1]);
            return Vector3.Lerp(s[0].Position, s[1].Position, u);
        }

        private Vector3 ExtrapolateFromLastState(TransformState last, double t)
        {
            double delta = Mathf.Min(interpolationBackTime, (float)(t - last.Timestamp));
            return last.Position
                 + ComputeAverageVelocity() * (float)delta * extrapolationMultiplier;
        }

        private void AddStateSnapshot(Vector3 pos, double time)
        {
            _stateSnapshots.Enqueue(new TransformState { Position = pos, Timestamp = time });
            if (_stateSnapshots.Count > maxBufferSize)
                _stateSnapshots.Dequeue();
        }

        private void RecordVelocity(TransformState older, TransformState newer)
        {
            double dt = newer.Timestamp - older.Timestamp;
            if (dt <= 0) return;

            Vector3 vel = (newer.Position - older.Position) / (float)dt;
            _velocityBuffer.Enqueue(vel);
            if (_velocityBuffer.Count > velocityBufferSize)
                _velocityBuffer.Dequeue();
        }

        private Vector3 ComputeAverageVelocity()
        {
            if (_velocityBuffer.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var v in _velocityBuffer) sum += v;
            return sum / _velocityBuffer.Count;
        }

        private void ApplySmoothedPosition(Vector3 target)
        {
            float snapSq  = snapDistance * snapDistance;
            float sqrDist = (transform.position - target).sqrMagnitude;

            if (sqrDist > snapSq)
            {
                transform.position = target;
                _smoothVelocity    = Vector3.zero;
                _velocityBuffer.Clear();
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    target,
                    ref _smoothVelocity,
                    1f / smoothingSpeed
                );
            }
        }

        private Vector3 CatmullRom(
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
            );
        }

        protected override bool OnIsServerAuthoritative() => false;
    }
}



//The code below is for simple smoothing


// using Unity.Netcode.Components;
// using UnityEngine;
// using System.Collections.Generic;

// namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
// {
//     [DisallowMultipleComponent]
//     public class ClientMovementNetworkTransform: NetworkTransform
//     {
//         private struct State
//         {
//             public Vector3 Position;
//             public Quaternion Rotation;
//             public float Timestamp;
//         }

//         private Queue<State> stateBuffer = new Queue<State>();
//         private const float interpolationBackTime = 0.2f; // 200 ms buffer for bad networks
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
//             if (!IsOwner) // use (!IsOwner && !IsServer) if you want to allow host to use smoothing for remote objects, it does not own
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