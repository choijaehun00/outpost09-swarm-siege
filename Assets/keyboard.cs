using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class keyboard : MonoBehaviour
{
    public Rigidbody player;
    public float speed = 8f;

    private Vector3 moveDirection;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        var keys = Keyboard.current;
        moveDirection = Vector3.zero;

        if (keys == null) return;

        if (keys.upArrowKey.isPressed)
            moveDirection += Vector3.forward;

        if (keys.downArrowKey.isPressed)
            moveDirection += Vector3.back;

        if (keys.rightArrowKey.isPressed)
            moveDirection += Vector3.right;

        if (keys.leftArrowKey.isPressed)
            moveDirection += Vector3.left;

        // 대각선으로 이동할 때도 같은 크기의 힘 적용
        moveDirection = moveDirection.normalized;
    }

    private void FixedUpdate()
    {
        player.AddForce(moveDirection * speed, ForceMode.Force);
    }
}