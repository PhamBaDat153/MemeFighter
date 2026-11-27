using UnityEngine;
using UnityEngine.InputSystem;

namespace MemeFighter.Fight
{
    /// <summary>
    /// 2.5D platform-fighter movement: run, jump, double jump, fast fall, drop through platforms.
    /// Movement is locked to the X/Y plane. The model child turns to face left/right and gets
    /// procedural squash, bounce and lean until real animations exist.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class FighterMotor : MonoBehaviour
    {
        public enum Control { P1Keyboard, P2Keyboard, Cpu, None }

        [Header("Control")]
        public Control control = Control.P1Keyboard;
        public Transform cpuTarget;

        [Header("Movement")]
        public float runSpeed = 7f;
        public float airSpeed = 6f;
        public float groundAccel = 70f;
        public float airAccel = 35f;
        public float jumpSpeed = 11.5f;
        public float doubleJumpSpeed = 10.5f;
        public int airJumps = 1;
        public float gravity = 30f;
        public float fallMultiplier = 1.35f;
        public float maxFallSpeed = 18f;
        public float fastFallSpeed = 24f;

        [Header("Model")]
        public Transform model;
        [Tooltip("Yaw when facing right. 90 = exactly sideways, closer to 180 turns the face toward the camera.")]
        public float faceRightYaw = 120f;
        [Tooltip("Extra yaw if the imported model doesn't face +Z.")]
        public float modelYawOffset = 0f;

        [Header("Respawn")]
        public Vector3 spawnPoint;
        public float blastBottom = -12f, blastTop = 22f, blastSide = 26f;

        public int Facing { get; private set; } = 1;
        public bool Grounded { get; private set; }

        Rigidbody rb;
        CapsuleCollider col;
        float moveX;
        bool jumpQueued, downHeld, fastFall;
        int airJumpsLeft;
        float dropTimer;
        float bobTime, squash, squashVel, yaw;
        Vector3 modelBasePos, modelBaseScale = Vector3.one;
        bool wasGrounded;
        float cpuJumpCooldown;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            spawnPoint = transform.position;
            yaw = faceRightYaw;
        }

        void Update()
        {
            ReadInput();
            UpdateModel();
        }

        void ReadInput()
        {
            var kb = Keyboard.current;
            moveX = 0f;
            bool jump = false, down = false, downPressed = false;

            switch (control)
            {
                case Control.P1Keyboard when kb != null:
                    moveX = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
                    jump = kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                    down = kb.sKey.isPressed; downPressed = kb.sKey.wasPressedThisFrame;
                    break;
                case Control.P2Keyboard when kb != null:
                    moveX = (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);
                    jump = kb.upArrowKey.wasPressedThisFrame;
                    down = kb.downArrowKey.isPressed; downPressed = kb.downArrowKey.wasPressedThisFrame;
                    break;
                case Control.Cpu:
                    CpuThink(out moveX, out jump);
                    break;
            }

            if (jump) jumpQueued = true;
            downHeld = down;
            if (downPressed)
            {
                if (Grounded && StandingOnOneWay()) dropTimer = 0.3f;
                else if (!Grounded && rb.linearVelocity.y < 2f) fastFall = true;
            }
            if (moveX > 0.1f) Facing = 1;
            else if (moveX < -0.1f) Facing = -1;
        }

        void CpuThink(out float mx, out bool jump)
        {
            mx = 0f; jump = false;
            cpuJumpCooldown -= Time.deltaTime;
            Vector3 p = transform.position;

            // Off stage: head back to the middle and jump.
            if (Mathf.Abs(p.x) > 7.5f || p.y < -1f)
            {
                mx = -Mathf.Sign(p.x);
                if (rb.linearVelocity.y < 0f && cpuJumpCooldown <= 0f) { jump = true; cpuJumpCooldown = 0.35f; }
                return;
            }
            if (cpuTarget == null) return;
            float dx = cpuTarget.position.x - p.x;
            if (Mathf.Abs(dx) > 1.3f) mx = Mathf.Sign(dx);
            if (cpuTarget.position.y > p.y + 1.6f && Grounded && cpuJumpCooldown <= 0f)
            {
                jump = true; cpuJumpCooldown = 0.8f;
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Grounded = CheckGround();
            if (Grounded)
            {
                airJumpsLeft = airJumps;
                fastFall = false;
                if (!wasGrounded) squashVel -= 3.5f;   // land squish
            }
            wasGrounded = Grounded;

            Vector3 v = rb.linearVelocity;
            float target = moveX * (Grounded ? runSpeed : airSpeed);
            v.x = Mathf.MoveTowards(v.x, target, (Grounded ? groundAccel : airAccel) * dt);

            if (jumpQueued)
            {
                if (Grounded) { v.y = jumpSpeed; squashVel += 4f; Grounded = false; }
                else if (airJumpsLeft > 0) { airJumpsLeft--; v.y = doubleJumpSpeed; squashVel += 3f; fastFall = false; }
                jumpQueued = false;
            }

            if (!Grounded)
            {
                v.y -= gravity * (v.y < 0f ? fallMultiplier : 1f) * dt;
                v.y = Mathf.Max(v.y, fastFall ? -fastFallSpeed : -maxFallSpeed);
                if (fastFall) v.y = Mathf.Min(v.y, -fastFallSpeed * 0.8f);
            }
            else if (v.y < 0f) v.y = 0f;

            rb.linearVelocity = v;

            dropTimer -= dt;
            OneWayPlatform.UpdateFor(col, transform.position.y, v.y > 0.1f || dropTimer > 0f);

            Vector3 p = rb.position;
            if (p.y < blastBottom || p.y > blastTop || Mathf.Abs(p.x) > blastSide) Respawn();
        }

        bool CheckGround()
        {
            if (rb.linearVelocity.y > 0.1f) return false;
            float r = col.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (r + 0.05f);
            var hits = Physics.SphereCastAll(origin, r, Vector3.down, 0.12f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.collider == col || h.collider.attachedRigidbody == rb) continue;
                if (OneWayPlatform.IsOneWay(h.collider) && (dropTimer > 0f || transform.position.y < h.collider.bounds.max.y - 0.05f)) continue;
                return true;
            }
            return false;
        }

        bool StandingOnOneWay()
        {
            float r = col.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (r + 0.05f);
            if (Physics.SphereCast(origin, r, Vector3.down, out var hit, 0.15f, ~0, QueryTriggerInteraction.Ignore))
                return OneWayPlatform.IsOneWay(hit.collider);
            return false;
        }

        public void Respawn()
        {
            rb.position = spawnPoint + Vector3.up * 2f;
            rb.linearVelocity = Vector3.zero;
            transform.position = rb.position;
            fastFall = false;
            airJumpsLeft = airJumps;
        }

        bool modelInit;

        public void ForceFacing(int dir)
        {
            Facing = dir >= 0 ? 1 : -1;
            yaw = (Facing > 0 ? faceRightYaw : -faceRightYaw) + modelYawOffset;
        }

        void UpdateModel()
        {
            if (!model) return;
            if (!modelInit)
            {
                modelBasePos = model.localPosition;
                modelBaseScale = model.localScale;
                modelInit = true;
            }
            float dt = Time.deltaTime;
            float speed = Mathf.Abs(rb.linearVelocity.x) / runSpeed;

            // turn toward facing direction
            float targetYaw = (Facing > 0 ? faceRightYaw : -faceRightYaw) + modelYawOffset;
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, 900f * dt);

            // run bounce + lean
            float bob = 0f, lean = 0f;
            if (Grounded && speed > 0.1f)
            {
                bobTime += dt * (10f + 6f * speed);
                bob = Mathf.Abs(Mathf.Sin(bobTime)) * 0.07f * speed;
                lean = 10f * speed;
            }
            else if (!Grounded)
            {
                lean = Mathf.Clamp(rb.linearVelocity.y * -0.8f, -8f, 8f);
            }

            // squash & stretch spring
            squashVel += (-squash * 180f - squashVel * 14f) * dt;
            squash += squashVel * dt;
            float sy = 1f + squash * 0.25f;
            float sxz = 1f - squash * 0.12f;

            model.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(lean, 0f, 0f);
            model.localPosition = modelBasePos + Vector3.up * bob;
            model.localScale = new Vector3(modelBaseScale.x * sxz, modelBaseScale.y * sy, modelBaseScale.z * sxz);
        }
    }
}
