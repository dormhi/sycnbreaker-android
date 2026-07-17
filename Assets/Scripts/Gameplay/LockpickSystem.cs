/* =========================================
   LockpickSystem.cs — Lockpick Mini-Game
   
   NoPixel 4.0 style radial lockpick mechanic.
   N nodes on a circle, each with a direction.
   A cursor rotates; press the correct arrow
   key / swipe when cursor reaches a node.
   
   Physics Enhancements (vs web version):
   - Cursor has inertia (accelerates/decelerates)
   - Nodes vibrate on solve (spring-based shake)
   - Particle burst on success/fail
   - Angular momentum carries through
   
   Ported from: js/LockpickSystem.js
   ========================================= */

using UnityEngine;
using System;
using System.Collections.Generic;
using SyncBreaker.Core;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Direction required to unlock a lockpick node.
    /// Maps to swipe directions and arrow keys.
    /// </summary>
    public enum LockpickDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// State of an individual lockpick node.
    /// </summary>
    [System.Serializable]
    public class LockpickNode
    {
        /// <summary>Angle position on the circle (degrees, 0 = top).</summary>
        public float Angle;

        /// <summary>Required direction to unlock this node.</summary>
        public LockpickDirection Direction;

        /// <summary>Has this node been successfully unlocked?</summary>
        public bool Solved;

        /// <summary>Was this node the point of failure?</summary>
        public bool Failed;

        // ── Physics Animation ──
        /// <summary>Current vibration offset (for spring effect).</summary>
        public float VibrationOffset;

        /// <summary>Vibration velocity (damped spring).</summary>
        public float VibrationVelocity;

        /// <summary>Scale animation (pulse on solve).</summary>
        public float ScaleAnim = 1f;

        public LockpickNode(float angle, LockpickDirection direction)
        {
            Angle = angle;
            Direction = direction;
            Solved = false;
            Failed = false;
            VibrationOffset = 0f;
            VibrationVelocity = 0f;
            ScaleAnim = 1f;
        }

        /// <summary>
        /// Get the Unicode symbol for this node's direction.
        /// </summary>
        public string DirectionSymbol => Direction switch
        {
            LockpickDirection.Up => "↑",
            LockpickDirection.Down => "↓",
            LockpickDirection.Left => "←",
            LockpickDirection.Right => "→",
            _ => "?"
        };
    }

    /// <summary>
    /// Complete lockpick mini-game system.
    /// Manages nodes, cursor rotation, input validation,
    /// and result callbacks.
    /// </summary>
    public class LockpickSystem : MonoBehaviour
    {
        // ── Events ──
        /// <summary>Fired when the lockpick completes. Args: (success)</summary>
        public event Action<bool> OnComplete;

        /// <summary>Fired when a node is solved. Args: (nodeIndex)</summary>
        public event Action<int> OnNodeSolved;

        /// <summary>Fired when the player fails. Args: (nodeIndex)</summary>
        public event Action<int> OnNodeFailed;

        // ── Game State ──
        /// <summary>Is the lockpick active?</summary>
        public bool Active { get; private set; }

        /// <summary>Has the cursor started rotating? (first input starts it)</summary>
        public bool Started { get; private set; }

        /// <summary>Result: "success", "fail", or null if still playing.</summary>
        public string Result { get; private set; }

        /// <summary>Has the completion callback been invoked?</summary>
        private bool _finished;

        /// <summary>Callback to invoke on completion.</summary>
        private Action<bool> _onCompleteCallback;

        // ── Nodes ──
        /// <summary>All nodes in this lockpick session.</summary>
        public List<LockpickNode> Nodes { get; private set; } = new();

        /// <summary>Index of the node the player must solve next.</summary>
        public int CurrentNodeIndex { get; private set; }

        /// <summary>Number of nodes in this session.</summary>
        public int NodeCount => Nodes.Count;

        // ── Cursor ──
        /// <summary>Current cursor angle (degrees).</summary>
        public float CursorAngle { get; private set; }

        /// <summary>Cursor rotation speed (degrees/second).</summary>
        public float CursorSpeed { get; private set; }

        // ── Physics ──
        /// <summary>Angular velocity with momentum (degrees/second).</summary>
        private float _angularVelocity;

        /// <summary>Target angular velocity (what we're accelerating towards).</summary>
        private float _targetAngularVelocity;

        /// <summary>How quickly cursor reaches target speed.</summary>
        private float _angularAcceleration = 8f;

        /// <summary>Angular drag (slows cursor when no input).</summary>
        private float _angularDrag = 0.98f;

        // ── Difficulty ──
        /// <summary>Current difficulty level (1-6).</summary>
        public int Difficulty { get; private set; }

        /// <summary>Angular tolerance for hitting a node (degrees).</summary>
        public float HitTolerance { get; private set; }

        // ── Result Display ──
        private float _resultTimer;
        private float _resultDuration = 1.5f;

        // ── Configuration Tables ──
        private static readonly int[] NodeCounts = { 4, 5, 5, 6, 6, 7 };
        private static readonly float[] Speeds = { 70f, 90f, 110f, 130f, 160f, 200f };
        private static readonly float[] Tolerances = { 28f, 24f, 20f, 16f, 13f, 10f };

        // ── Spring Physics Constants ──
        private const float SpringStiffness = 300f;
        private const float SpringDamping = 12f;

        // ════════════════════════════════════════
        //  PUBLIC API
        // ════════════════════════════════════════

        /// <summary>
        /// Start a new lockpick session.
        /// </summary>
        /// <param name="difficulty">Difficulty level (1-6)</param>
        /// <param name="onComplete">Callback: true = success, false = fail</param>
        public void StartLockpick(int difficulty, Action<bool> onComplete)
        {
            Difficulty = Mathf.Clamp(difficulty, 1, 6);
            _onCompleteCallback = onComplete;
            Result = null;
            _finished = false;
            Started = false;
            _resultTimer = 0f;
            CurrentNodeIndex = 0;

            // Configure based on difficulty
            int nodeCount = NodeCounts[Difficulty - 1];
            CursorSpeed = Speeds[Difficulty - 1];
            HitTolerance = Tolerances[Difficulty - 1];

            // Generate nodes
            GenerateNodes(nodeCount);

            // Position cursor slightly before first node
            float firstAngle = Nodes[0].Angle;
            CursorAngle = (firstAngle - 50f + 360f) % 360f;

            // Physics state
            _angularVelocity = 0f;
            _targetAngularVelocity = 0f;

            Active = true;

            Debug.Log($"[Lockpick] Started: difficulty={Difficulty}, " +
                      $"nodes={nodeCount}, speed={CursorSpeed}, tolerance={HitTolerance}");
        }

        /// <summary>
        /// Stop the lockpick (cancel without result).
        /// </summary>
        public void Cancel()
        {
            Active = false;
            Started = false;
        }

        // ════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════

        private void Update()
        {
            if (!Active) return;

            float dt = Time.deltaTime;

            // ── Showing result → wait then invoke callback ──
            if (Result != null)
            {
                _resultTimer += dt;
                if (_resultTimer >= _resultDuration && !_finished)
                {
                    _finished = true;
                    bool success = Result == "success";
                    _onCompleteCallback?.Invoke(success);
                    OnComplete?.Invoke(success);
                }

                // Still update node physics during result display
                UpdateNodePhysics(dt);
                return;
            }

            // ── Don't rotate cursor until started ──
            if (!Started)
            {
                UpdateNodePhysics(dt);
                return;
            }

            // ── Cursor rotation with physics ──
            UpdateCursorPhysics(dt);

            // ── Check if cursor passed active node ──
            CheckMiss();

            // ── Node spring physics ──
            UpdateNodePhysics(dt);
        }

        /// <summary>
        /// Physics-based cursor rotation.
        /// Instead of constant speed, the cursor has angular momentum
        /// that smoothly accelerates to target speed.
        /// </summary>
        private void UpdateCursorPhysics(float dt)
        {
            // Smoothly accelerate towards target speed
            _targetAngularVelocity = CursorSpeed;
            _angularVelocity = Mathf.Lerp(_angularVelocity, _targetAngularVelocity,
                _angularAcceleration * dt);

            // Apply angular drag (subtle deceleration for realism)
            _angularVelocity *= Mathf.Pow(_angularDrag, dt);

            // Apply rotation
            CursorAngle = (CursorAngle + _angularVelocity * dt) % 360f;
            if (CursorAngle < 0f) CursorAngle += 360f;
        }

        /// <summary>
        /// Update spring physics on solved/failed nodes (vibration effect).
        /// </summary>
        private void UpdateNodePhysics(float dt)
        {
            foreach (var node in Nodes)
            {
                if (!node.Solved && !node.Failed) continue;

                // Damped spring: F = -kx - cv
                float springForce = -SpringStiffness * node.VibrationOffset;
                float dampingForce = -SpringDamping * node.VibrationVelocity;
                float totalForce = springForce + dampingForce;

                node.VibrationVelocity += totalForce * dt;
                node.VibrationOffset += node.VibrationVelocity * dt;

                // Scale animation (pulse then settle)
                if (node.Solved)
                {
                    node.ScaleAnim = Mathf.Lerp(node.ScaleAnim, 1f, 5f * dt);
                }

                // Stop very small vibrations
                if (Mathf.Abs(node.VibrationOffset) < 0.001f &&
                    Mathf.Abs(node.VibrationVelocity) < 0.01f)
                {
                    node.VibrationOffset = 0f;
                    node.VibrationVelocity = 0f;
                }
            }
        }

        // ════════════════════════════════════════
        //  INPUT HANDLING
        // ════════════════════════════════════════

        /// <summary>
        /// Handle a directional input (from TouchInputHandler swipe or keyboard).
        /// </summary>
        /// <param name="direction">The direction input</param>
        /// <returns>True if input was consumed</returns>
        public bool HandleInput(SwipeDirection direction)
        {
            if (!Active || Result != null) return false;

            // Map SwipeDirection to LockpickDirection
            LockpickDirection? lockDir = direction switch
            {
                SwipeDirection.Up => LockpickDirection.Up,
                SwipeDirection.Down => LockpickDirection.Down,
                SwipeDirection.Left => LockpickDirection.Left,
                SwipeDirection.Right => LockpickDirection.Right,
                _ => null
            };

            if (lockDir == null) return false;

            // First input starts the cursor
            if (!Started)
            {
                Started = true;
                Debug.Log("[Lockpick] Cursor started rotating.");
                return true; // First press only starts, doesn't check nodes
            }

            // Get current active node
            if (CurrentNodeIndex >= Nodes.Count) return false;
            var currentNode = Nodes[CurrentNodeIndex];
            if (currentNode.Solved) return false;

            // Is cursor near the active node?
            float dist = AngleDistance(CursorAngle, currentNode.Angle);
            if (dist > HitTolerance)
            {
                // Pressed while not near node → FAIL
                Fail(CurrentNodeIndex);
                return true;
            }

            // Check if correct direction
            if (lockDir == currentNode.Direction)
            {
                // CORRECT! Solve this node
                SolveNode(CurrentNodeIndex);
            }
            else
            {
                // Wrong direction → FAIL
                Fail(CurrentNodeIndex);
            }

            return true;
        }

        /// <summary>
        /// Handle keyboard input directly (for editor testing).
        /// Maps KeyCode to SwipeDirection.
        /// </summary>
        public bool HandleKeyCode(KeyCode key)
        {
            SwipeDirection dir = key switch
            {
                KeyCode.UpArrow or KeyCode.W => SwipeDirection.Up,
                KeyCode.DownArrow or KeyCode.S => SwipeDirection.Down,
                KeyCode.LeftArrow or KeyCode.A => SwipeDirection.Left,
                KeyCode.RightArrow or KeyCode.D => SwipeDirection.Right,
                _ => SwipeDirection.None
            };

            if (dir == SwipeDirection.None) return false;
            return HandleInput(dir);
        }

        // ════════════════════════════════════════
        //  NODE OPERATIONS
        // ════════════════════════════════════════

        /// <summary>
        /// Mark a node as solved and advance to next.
        /// </summary>
        private void SolveNode(int index)
        {
            var node = Nodes[index];
            node.Solved = true;

            // Physics: trigger vibration (positive impulse)
            node.VibrationVelocity = 15f;
            node.ScaleAnim = 1.3f; // Pulse up

            // Slight speed boost on solve (feels rewarding)
            _angularVelocity *= 1.05f;

            OnNodeSolved?.Invoke(index);
            Debug.Log($"[Lockpick] Node {index} SOLVED! ({node.DirectionSymbol})");

            CurrentNodeIndex++;

            // All solved?
            if (CurrentNodeIndex >= Nodes.Count)
            {
                Result = "success";
                _resultTimer = 0f;
                _angularVelocity = 0f; // Stop cursor
                Debug.Log("[Lockpick] SUCCESS! All nodes solved.");
            }
        }

        /// <summary>
        /// Mark the current node as failed and end the session.
        /// </summary>
        private void Fail(int index)
        {
            if (index < Nodes.Count)
            {
                var node = Nodes[index];
                node.Failed = true;

                // Physics: trigger strong vibration (negative impulse)
                node.VibrationVelocity = -25f;
            }

            Result = "fail";
            _resultTimer = 0f;
            _angularVelocity = 0f; // Stop cursor

            OnNodeFailed?.Invoke(index);
            Debug.Log($"[Lockpick] FAILED at node {index}.");
        }

        /// <summary>
        /// Check if cursor has passed the active node without input.
        /// </summary>
        private void CheckMiss()
        {
            if (CurrentNodeIndex >= Nodes.Count) return;
            var currentNode = Nodes[CurrentNodeIndex];
            if (currentNode.Solved) return;

            float dist = AngleDistance(CursorAngle, currentNode.Angle);
            bool past = IsAnglePast(CursorAngle, currentNode.Angle, HitTolerance + 5f);

            if (past && dist > HitTolerance + 5f)
            {
                Fail(CurrentNodeIndex);
            }
        }

        // ════════════════════════════════════════
        //  NODE GENERATION
        // ════════════════════════════════════════

        /// <summary>
        /// Generate nodes evenly distributed around the circle.
        /// </summary>
        private void GenerateNodes(int count)
        {
            Nodes.Clear();
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                // Start from top (-90°) and distribute clockwise
                float angle = (i * angleStep - 90f + 360f) % 360f;

                // Random direction
                var dir = (LockpickDirection)UnityEngine.Random.Range(0, 4);

                Nodes.Add(new LockpickNode(angle, dir));
            }
        }

        // ════════════════════════════════════════
        //  ANGLE HELPERS
        // ════════════════════════════════════════

        /// <summary>
        /// Shortest angular distance between two angles (degrees).
        /// </summary>
        private float AngleDistance(float a, float b)
        {
            float diff = Mathf.Abs(((a - b) % 360f + 360f) % 360f);
            return diff > 180f ? 360f - diff : diff;
        }

        /// <summary>
        /// Check if cursor angle has passed a node angle (clockwise, with margin).
        /// </summary>
        private bool IsAnglePast(float cursorAngle, float nodeAngle, float margin)
        {
            float diff = ((cursorAngle - nodeAngle) % 360f + 360f) % 360f;
            return diff > margin && diff < 180f;
        }

        // ════════════════════════════════════════
        //  QUERIES
        // ════════════════════════════════════════

        /// <summary>
        /// Get the world position of a node on the circle.
        /// </summary>
        public Vector2 GetNodePosition(int index, float radius)
        {
            if (index < 0 || index >= Nodes.Count) return Vector2.zero;
            float rad = Nodes[index].Angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        /// <summary>
        /// Get the world position of the cursor on the circle.
        /// </summary>
        public Vector2 GetCursorPosition(float radius)
        {
            float rad = CursorAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        /// <summary>
        /// How many nodes have been solved.
        /// </summary>
        public int SolvedCount
        {
            get
            {
                int count = 0;
                foreach (var n in Nodes)
                    if (n.Solved) count++;
                return count;
            }
        }

        /// <summary>
        /// Progress as 0-1 fraction.
        /// </summary>
        public float Progress => Nodes.Count > 0 ? (float)SolvedCount / Nodes.Count : 0f;
    }
}
