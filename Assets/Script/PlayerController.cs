using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Properties")]
    [SerializeField] private SpriteRenderer graphic;

    [Header("Status")]
    public float health = 100;
    public float attack = 5;

    public bool canBeMoved = true;

    public float healthMax { private set; get; }

    private bool isGrounded = true;
    private bool isShooting = true;
    private bool doubleJump;
    private int velocityX = 0;
    private int velocityY = 0;

    [Header("Configuration")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private float shootingTimeMax = 1.0f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10;

    [SerializeField] private Vector2 gunOffset;

    private float shootingTime = 0;

    [SerializeField] private float minGroundDistance = 1.5f;

    [Header("SFV")]
    private AudioSource playerAudio;
    public AudioClip playerJump;
    public AudioClip playerShoot;
    public AudioClip playerMove;
    public AudioClip playerHit;

    private Rigidbody2D rb2;
    private Animator animator;

    bool trigMove = false;
    bool trigShot = true;

    Vector2 direction;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb2 = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<AudioSource>();

        shootingTime = shootingTimeMax;
        healthMax = health;
    }

    void Update()
    {
        if (canBeMoved)
        {
            MovementController();
            JumpController();
            ShootController();

            AnimationController();
        }

        FallDie();
    }

    void MovementController()
    {
        float x = Mathf.Round(Input.GetAxisRaw("Horizontal"));

        if (x != 0)
        {
            trigMove = true;
            moving(x);
        }
        else if (trigMove)
        {
            trigMove = false;
            moving(0);
        }
    }

    public void moving(float x)
    {
        direction = rb2.velocity;
        direction.x = x * moveSpeed;

        rb2.velocity = direction;

        //sprite dibalik ketika arahnya ke kiri
        if (direction.x < 0)
        {
            graphic.flipX = true;
        }
        else if (direction.x > 0)
        {
            graphic.flipX = false;
        }
    }

    void JumpController()
    {
        RaycastHit2D ray = Physics2D.Raycast(transform.position, Vector2.down, 10, LayerMask.GetMask("Obstacle"));

        if (ray && ray.distance < minGroundDistance)
        {
            isGrounded = true;
            if (rb2.velocity.y == 0)
                doubleJump = false;
        }
        else
        {
            isGrounded = false;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Jump();
        }

    }

    public void Jump()
    {
        if (isGrounded || doubleJump)
        {
            rb2.velocity = new Vector2(rb2.velocity.x, jumpForce);
            playerAudio.PlayOneShot(playerJump, 1.0f);
            doubleJump = !doubleJump;
        }
    }

    void ShootController()
    {
        isShooting = Input.GetKey(KeyCode.Z);

        if (isShooting)
        {
            shootingTime -= Time.deltaTime;
            if (trigShot)
            {
                Shoot();
                trigShot = false;
            }

        }
        else
        {
            shootingTime = shootingTimeMax;
            trigShot = true;
        }

        if (isShooting && shootingTime < 0)
        {
            shootingTime = shootingTimeMax;
            Shoot();
        }
    }

    void Shoot()
    {
        playerAudio.PlayOneShot(playerShoot, 1.0f);

        int direction = (graphic.flipX == false ? 1 : -1);

        Vector2 gunPos = new Vector2(gunOffset.x * direction + transform.position.x, gunOffset.y + transform.position.y);

        GameObject bulletObj = Instantiate(bulletPrefab, gunPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet)
        {
            bullet.Launch(new Vector2(direction, 0), "Enemy", bulletSpeed, attack);
        }
    }

    public void DamagedBy(float damage)
    {
        playerAudio.PlayOneShot(playerHit, 1.0f);

        health -= damage;
        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    void FallDie()
    {
        if (transform.position.y < -20)
        {
            Die();
        }
    }

    void Die()
    {
        graphic.enabled = false;
        canBeMoved = false;
        GameManager.GameOver();
    }

    void AnimationController()
    {
        velocityX = (int)Mathf.Clamp(rb2.velocity.x, -1, 1);
        velocityY = (int)Mathf.Clamp(rb2.velocity.y, -1, 1);

        animator.SetBool("isGrounded", isGrounded);
        animator.SetInteger("velocityX", velocityX);
        animator.SetInteger("velocityY", velocityY);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Enemy")
        {
            float damage = collision.collider.GetComponent<EnemyController>().GetAttackDamage();
            DamagedBy(damage);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Bullet")
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet.targetTag == "Player")
            {
                float damage = bullet.GetDamage();
                DamagedBy(damage);
                Destroy(collision.gameObject);
            }
        }
    }

    public void MoveSFXStop()
    {
        playerAudio.Stop();
    }

    // Joystick Controller
    void OnMove(InputValue value)
    {
        moving(Mathf.Round(value.Get<Vector2>().x));
    }

    void OnAttack()
    {
        Shoot();
    }

    void OnJump()
    {
        Jump();
    }

}