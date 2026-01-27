using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player :MonoBehaviour
{
    private Vector2 moveInput;
    public float speed = 10f;
    Animator animator;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(moveInput.x),
                1,
                1
            );
        }
        animator.SetFloat("velocity", moveInput.magnitude);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * speed;
    }
}
