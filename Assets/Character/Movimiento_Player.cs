using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Movimiento_Player : MonoBehaviour
{
    [Header("Movimiento")]
    public float WalkSpeed = 4f;
    public float SprintSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Rotación del personaje")]
    public Transform modelo;            // arrastra aquí "Personaje" (si lo dejas vacío, gira todo el Player)
    public float velocidadGiro = 12f;

    [Header("Referenciación")]
    public Transform cameraTransform;   // la cámara isométrica
    public Animator animator;

    private CharacterController characterController;
    private Vector3 Velocity = Vector3.zero;
    private Vector3 externalVelocity = Vector3.zero;
    private Vector3 moveDirection = Vector3.zero;

    private float currentSpeed;

    public bool IsMoving { get; private set; }
    public Vector2 CurrentInput { get; private set; }
    public bool IsGrounded { get; private set; }

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (modelo == null)
            modelo = transform;
    }

    void Update()
    {
        IsGrounded = characterController.isGrounded;

        if (IsGrounded && Velocity.y < 0f)
        {
            Velocity.y = -2f;
        }

        HandleMovement();
        RotarHaciaMovimiento();

        Velocity.y += gravity * Time.deltaTime;

        Vector3 finalMovement = moveDirection * currentSpeed + externalVelocity;
        finalMovement.y = Velocity.y;

        characterController.Move(finalMovement * Time.deltaTime);

        IsGrounded = characterController.isGrounded;

        if (IsGrounded && Velocity.y < 0f)
        {
            SetBoolSeguro("isJumping", false);
        }

        UpdateAnimator();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        CurrentInput = new Vector2(horizontal, vertical);

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        IsMoving = inputDirection.magnitude > 0.1f;

        moveDirection = Vector3.zero;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isSprinting ? SprintSpeed : WalkSpeed;

        if (IsMoving)
        {
            if (cameraTransform != null)
            {
                // Relativo a hacia dónde mira la cámara isométrica
                moveDirection = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * inputDirection;
            }
            else
            {
                moveDirection = inputDirection;
            }
        }

        if (Input.GetButtonDown("Jump") && IsGrounded)
        {
            Velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            SetBoolSeguro("isJumping", true);
        }
    }

    void RotarHaciaMovimiento()
    {
        if (!IsMoving || moveDirection.sqrMagnitude < 0.001f) return;

        Quaternion objetivo = Quaternion.LookRotation(moveDirection, Vector3.up);

        modelo.rotation = Quaternion.Slerp(
            modelo.rotation,
            objetivo,
            velocidadGiro * Time.deltaTime
        );
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speedPercent = 0f;

        if (IsMoving)
        {
            speedPercent = currentSpeed == SprintSpeed ? 1f : 0.5f;
        }

        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", IsGrounded);
        animator.SetFloat("VerticalSpeed", Velocity.y);
    }

    // Solo cambia el bool si el parámetro existe (evita el aviso de "isJumping does not exist")
    void SetBoolSeguro(string nombre, bool valor)
    {
        if (animator == null) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == nombre)
            {
                animator.SetBool(nombre, valor);
                return;
            }
        }
    }

    public void SetExternalVelocity(Vector3 platformVelocity)
    {
        externalVelocity = platformVelocity;
    }
}