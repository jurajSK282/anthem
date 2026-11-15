using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Slow, weighty first-person walk (WASD + mouse, Shift = sprint, Space = jump, Q = dash).
// Movement only; combat/stamina live elsewhere (hook in via canDash / OnDash / OnJump).
[RequireComponent(typeof(CharacterController))]
public class FirstPersonMotor : MonoBehaviour
{
    [Header("Move")]
    public float walkSpeed = 3.6f;
    public float sprintSpeed = 6.2f;
    public float groundAcceleration = 16f;
    public float groundDeceleration = 20f;
    [Range(0f, 1f)] public float airControl = 0.35f;   // fraction of ground accel/decel while airborne

    [Header("Jump")]
    public float jumpHeight = 1.1f;
    public float gravity = 22f;
    public float fallGravityMultiplier = 1.4f;         // heavier on the way down
    public float jumpCutMultiplier = 2.2f;             // release Space early = shorter hop
    public float coyoteTime = 0.1f;                    // can still jump just after leaving a ledge
    public float jumpBuffer = 0.12f;                   // Space pressed just before landing still counts

    [Header("Dash")]
    public Key dashKey = Key.Q;
    public float dashSpeed = 14f;
    public float dashEndSpeed = 5f;                    // speed you carry out of the dash
    public float dashDuration = 0.22f;
    public float dashCooldown = 0.8f;
    public int maxAirDashes = 1;

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.1f;
    public float bobAmount = 0.045f;
    public float bobFrequency = 1.9f;
    public float sprintFovKick = 5f;
    public float dashFovKick = 14f;
    public float fovLerp = 10f;
    public float landDipAmount = 0.012f;               // camera dip per unit of landing speed
    public float landDipMax = 0.12f;
    public float landRecover = 9f;

    // Hooks for your stamina/combat scripts
    public Func<bool> canDash;                         // return false to block a dash (e.g. no stamina)
    public event Action OnDash;
    public event Action OnJump;
    public event Action<float> OnLand;                 // passes landing speed

    public bool IsDashing => dashTimer > 0f;
    public bool IsGrounded => controller != null && controller.isGrounded;

    CharacterController controller;
    Camera cam;
    Vector3 planar;
    float fall, pitch, bobTime, eyeHeight, bobBlend, baseFov, landDip;
    float coyoteCounter, bufferCounter;
    float dashTimer, dashCooldownTimer;
    Vector3 dashDir;
    int airDashesLeft;
    bool wasGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform != null)
        {
            eyeHeight = cameraTransform.localPosition.y;
            cam = cameraTransform.GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
        }
        airDashesLeft = maxAirDashes;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard kb = Keyboard.current;
        if (mouse == null || kb == null) return;
        float dt = Time.deltaTime;

        // ---- Look ----
        Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);

        // ---- Input ----
        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 wish = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        bool hasInput = wish.sqrMagnitude > 0.001f;
        bool sprinting = kb.leftShiftKey.isPressed && z > 0f;   // sprint only when moving forward-ish
        float speed = sprinting ? sprintSpeed : walkSpeed;

        bool grounded = controller.isGrounded;

        // ---- Land / ground state ----
        if (grounded && !wasGrounded)
        {
            float impact = Mathf.Max(0f, -fall);
            landDip = Mathf.Min(impact * landDipAmount, landDipMax);
            airDashesLeft = maxAirDashes;
            OnLand?.Invoke(impact);
        }
        if (grounded) airDashesLeft = Mathf.Max(airDashesLeft, maxAirDashes);
        wasGrounded = grounded;

        // ---- Jump (coyote time + input buffer) ----
        coyoteCounter = grounded ? coyoteTime : coyoteCounter - dt;
        bufferCounter = kb.spaceKey.wasPressedThisFrame ? jumpBuffer : bufferCounter - dt;

        if (grounded && fall < 0f) fall = -2f;

        if (bufferCounter > 0f && coyoteCounter > 0f)
        {
            fall = Mathf.Sqrt(2f * gravity * jumpHeight);
            bufferCounter = 0f;
            coyoteCounter = 0f;
            OnJump?.Invoke();
        }

        // ---- Dash ----
        dashCooldownTimer -= dt;
        if (kb[dashKey].wasPressedThisFrame && !IsDashing && dashCooldownTimer <= 0f
            && (grounded || airDashesLeft > 0)
            && (canDash == null || canDash()))
        {
            dashDir = hasInput ? wish.normalized : transform.forward;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            if (!grounded) { airDashesLeft--; fall = Mathf.Max(fall, 0f); } // kill downward speed so air dash stays level
            OnDash?.Invoke();
        }

        if (IsDashing)
        {
            dashTimer -= dt;
            float t = 1f - Mathf.Clamp01(dashTimer / dashDuration);
            float dashNow = Mathf.Lerp(dashSpeed, dashEndSpeed, t * t);   // fast burst, eases out
            planar = dashDir * dashNow;
        }
        else
        {
            // Accelerate toward input, decelerate (friction) when no input.
            // Different rates + reduced air control = smooth, weighty feel with momentum preserved in the air.
            float rate = hasInput ? groundAcceleration : groundDeceleration;
            if (!grounded) rate *= airControl;
            planar = Vector3.MoveTowards(planar, wish * speed, rate * dt);
        }

        // ---- Gravity ----
        if (IsDashing && !grounded)
        {
            fall = 0f;   // float level during an air dash
        }
        else if (!grounded || fall > 0f)
        {
            float g = gravity;
            if (fall < 0f) g *= fallGravityMultiplier;
            else if (!kb.spaceKey.isPressed) g *= jumpCutMultiplier;   // variable jump height
            fall -= g * dt;
        }

        controller.Move((planar + Vector3.up * fall) * dt);

        // ---- Camera ----
        if (cameraTransform == null) return;

        float planarSpeed = planar.magnitude;
        float bobTarget = (grounded && !IsDashing) ? Mathf.Clamp01(planarSpeed / walkSpeed) : 0f;
        bobBlend = Mathf.MoveTowards(bobBlend, bobTarget, 6f * dt);
        bobTime += planarSpeed * dt * bobFrequency;
        float bob = Mathf.Sin(bobTime * Mathf.PI) * bobAmount * bobBlend;

        landDip = Mathf.Lerp(landDip, 0f, landRecover * dt);

        cameraTransform.localPosition = new Vector3(0f, eyeHeight + bob - landDip, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (cam != null)
        {
            float targetFov = baseFov + (IsDashing ? dashFovKick : (sprinting && hasInput ? sprintFovKick : 0f));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerp * dt);
        }
    }
}