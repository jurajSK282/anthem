using UnityEngine;
using UnityEngine.InputSystem;

// Slow, weighty first-person walk (WASD + mouse, Shift = sprint). Movement only; combat/stamina live elsewhere.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonMotor : MonoBehaviour
{
    public float walkSpeed = 3.6f;
    public float sprintSpeed = 6.2f;
    public float acceleration = 9f;
    public float gravity = 22f;
    public float mouseSensitivity = 0.1f;
    public Transform cameraTransform;
    public float bobAmount = 0.045f;
    public float bobFrequency = 1.9f;

    CharacterController controller;
    Vector3 planar;
    float fall, pitch, bobTime, eyeHeight;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform != null) eyeHeight = cameraTransform.localPosition.y;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard kb = Keyboard.current;
        if (mouse == null || kb == null) return;

        Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);

        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 wish = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        float speed = kb.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
        planar = Vector3.MoveTowards(planar, wish * speed, acceleration * Time.deltaTime);

        fall = controller.isGrounded ? -2f : fall - gravity * Time.deltaTime;
        controller.Move((planar + Vector3.up * fall) * Time.deltaTime);

        if (cameraTransform == null) return;
        bobTime += planar.magnitude * Time.deltaTime * bobFrequency;
        float bob = Mathf.Sin(bobTime * Mathf.PI) * bobAmount * Mathf.Clamp01(planar.magnitude / walkSpeed);
        cameraTransform.localPosition = new Vector3(0f, eyeHeight + bob, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
