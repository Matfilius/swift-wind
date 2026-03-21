using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Player Component References")]
    [SerializeField] Rigidbody2D rb;
    Animator animator;

    [Header("Player Settings")]
    [SerializeField] float dashLenght;
    [SerializeField] float speed;
    [SerializeField] float jumpingPower;
    [SerializeField] float dashDuration = 0.2f;
    public GameObject afterImagePrefab;
    public float afterImageSpacing = 0.05f;
    private float afterImageTimer;
    private bool isDashing;
    float horizontalInput;
    bool isFacingRight = true;
    private SpriteRenderer playerSR;

    public float rollSpeed = 40f;
    public float rollDuration = 5f;

    bool isRolling = false;
   


    [Header("Grounding")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;
    public bool doubleJump;
    [SerializeField] BoxCollider2D playerCollider;

    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    private float horizontal;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerSR = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<BoxCollider2D>();

        originalColliderSize = playerCollider.size;
        originalColliderOffset = playerCollider.offset;
    }
    void Update()
    {
        horizontalInput = horizontal;
        Flip();      
        

        if (isDashing)
        {
            afterImageTimer -= Time.deltaTime;

            if (afterImageTimer <= 0)
            {
                SpawnAfterImage();
                afterImageTimer = afterImageSpacing;
            }
        }

        

    }


    private void FixedUpdate()
    {
        if (isDashing) return;
        if (!isRolling)
        {
            rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
        }
        animator.SetFloat("xVelocity", Math.Abs(rb.linearVelocity.x));
        animator.SetFloat("yVelocity", (rb.linearVelocity.y));

    }



    #region PLAYER MOVEMENT

    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (context.performed && !isDashing && isGrounded() &&!isRolling)
        {
            StartCoroutine(DashRoutine());
        }

    }

    public void Roll(InputAction.CallbackContext context)
    {
        if (context.performed && !isRolling && isGrounded() && !isDashing)
        {
            StartCoroutine(RollMovement());
        }

    }



    IEnumerator RollMovement()
    {
        animator.SetBool("roll", true);
        isRolling = true;

        playerCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
        playerCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y);

        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Player"),LayerMask.NameToLayer("Enemy"),true
        );

        float rollDirection = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(rollDirection * rollSpeed, 0f);

        yield return new WaitForSeconds(rollDuration);

        playerCollider.size = originalColliderSize;
        playerCollider.offset = originalColliderOffset;

        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Player"),LayerMask.NameToLayer("Enemy"),false
        );

        isRolling = false;


        animator.SetBool("roll", false);

    }

    void Flip()
    {
        if (isFacingRight && horizontalInput < 0f || !isFacingRight && horizontalInput > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1f;
            transform.localScale = ls;

        }
    }

    public void Jump(InputAction.CallbackContext context)
    {

        if (context.performed)
        {
            animator.SetBool("isJumping", true);

            if (isGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                
                doubleJump = true; // dozvoli drugi skok
            }
            else if (doubleJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                //animator.SetBool("isJumping",!isGrounded());
                doubleJump = false; // potroši double jump
            }
        }
        
    }

    private bool isGrounded()
    {
        return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(0.64f, 0.1f), CapsuleDirection2D.Horizontal, 0, groundLayer);
       
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        animator.SetBool("isJumping", false);
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;

        float dashDirection = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dashDirection * dashLenght, 0f);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    #endregion

    #region EFFECTS

    void SpawnAfterImage()
    {
        GameObject obj = Instantiate(afterImagePrefab, transform.position, transform.rotation);

        SpriteRenderer playerSR = GetComponent<SpriteRenderer>();
        SpriteRenderer objSR = obj.GetComponent<SpriteRenderer>();

        objSR.sprite = playerSR.sprite;
        objSR.flipX = playerSR.flipX;
        objSR.flipX = playerSR.flipX;
        obj.transform.localScale = transform.localScale * 1.05f;
        objSR.color = new Color(0.3f, 0.8f, 1f, 0.4f);
    }

    #endregion
}
