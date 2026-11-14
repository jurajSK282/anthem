using UnityEngine;
using UnityEngine.InputSystem;

// Projekt ma aktivny Input System (activeInputHandler: 1), preto sa nepouziva Input.GetAxis.
[RequireComponent(typeof(CharacterController))]
public class SimpleFPController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f;
    public Transform cameraTransform;

    private CharacterController controller;
    private float verticalRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null || keyboard == null) return;

        // Otáčanie myšou
        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up * delta.x);
        verticalRotation = Mathf.Clamp(verticalRotation - delta.y, -80f, 80f);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Pohyb WASD + gravitácia
        float moveX = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float moveZ = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move((move * moveSpeed + Vector3.down * 9.81f) * Time.deltaTime);
    }
}
