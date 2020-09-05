﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    const float CameraMouseRotationSpeed = 12f;
    const float CameraControllerRotationSpeed = 3.0f;
    const float CameraXRotMin = -40.0f;
    const float CameraXRotMax = 30.0f;

    const float DirectionInterpolateSpeed = 1.0f;
    const float MotionInterpolateSpeed = 10.0f;
    const float RotationInterpolateSpeed = 10.0f;

    const float Speed = 12.0f;
    const float JumpForce = 20.0f;

    [SerializeField]
    float dashSpeed = 24.0f;
    [SerializeField]
    GameObject dashEffect;

	Quaternion modelRotation = Quaternion.identity;
	Quaternion aimRotation = Quaternion.identity;
	float cameraXRot = 0.0f;

    float horizontal = 0.0f;
    float vertical = 0.0f;

    float mouseX = 0f;
    float mouseY = 0f;

    const int maxHealth = 4;
    int health = maxHealth;

    bool onGround = false;
    bool attacking = false;
    bool dashing = false;
    bool hurt = false;

    bool lockMovement = false;
    public bool LockMovement { get => lockMovement; set => lockMovement = value; }

    public enum PaintColor
    {
        Red,
        Blue,
        Yellow
    }

    PaintColor currentColor = PaintColor.Red;
    public PaintColor CurrentColor { set => currentColor = value; get => currentColor; }

    new Camera camera;
    new Rigidbody rigidbody;
    Animator animator;
    AudioSource audioSource;

    SkinnedMeshRenderer modelMaterials;
    MeshRenderer bucketMaterials;

    [SerializeField]
    Material[] colorMaterials;

    [SerializeField]
    AudioClip dashSound;

    [SerializeField]
    AudioClip paintSound;

    [SerializeField]
    GameObject model = null;

    [SerializeField]
    GameObject cameraPivot = null;

    [SerializeField]
    GameObject cameraBase = null;

    [SerializeField]
    GameObject paintGlobsRed = null;

    [SerializeField]
    GameObject paintGlobBlue = null;

    [SerializeField]
    GameObject paintGlobsYellow = null;

    [SerializeField]
    LayerMask collisionMask;

    [SerializeField]
    bool redMouseAim, blueMouseAim, yellowMouseAim;

    void Start()
    {
        camera = GetComponentInChildren<Camera>();
        rigidbody = GetComponent<Rigidbody>();
        animator = model.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        modelMaterials = GetComponentInChildren<SkinnedMeshRenderer>();
        bucketMaterials = GetComponentInChildren<MeshRenderer>();

        Cursor.lockState = CursorLockMode.Locked;
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "EnemyHitbox")
        {
            HitboxCollision(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "EnemyHitbox")
        {
            HitboxCollision(other.gameObject);
        }
    }

    void HitboxCollision(GameObject hitbox)
    {
        float damage = 1;
        float knockback = 20;
        Damage(damage, -knockback*hitbox.transform.forward);
    }

    Vector3 knockback;
    void Damage(float amount, Vector3 knockbackDirection)
    {
        animator.SetBool("Hurt", true);
        knockback = knockbackDirection;
        modelRotation = Quaternion.LookRotation(-knockback);
        model.transform.rotation = modelRotation;
        health = Mathf.Clamp(--health, 0, maxHealth);
    }

    void Update()
    {
        GameUI.Singleton.SetHealth(health, maxHealth);

        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        attacking = animator.GetBool("InAttackState");
        dashing = animator.GetBool("InDashState");
        dashEffect.SetActive(dashing);
        hurt = animator.GetBool("InHurtState");
        if (attacking || hurt || lockMovement)
        {
            horizontal = 0;
            vertical = 0;
        }
        else
        {
            if (!dashing && Input.GetButtonDown("Fire3"))
            {
                Controller.Singleton.PlaySoundOneShot(dashSound, Random.Range(0.95f, 1.05f));
                Controller.Singleton.ShowComicText("whoosh", transform.position, camera);
                Dash();
            }
        }

		if (horizontal != 0f || vertical != 0f)
		{
			animator.SetFloat("Walking", 1);
		}
		else
		{
			animator.SetFloat("Walking", 0);
		}
		
		mouseX = Input.GetAxis("Mouse X");
		mouseY = Input.GetAxis("Mouse Y");

		if (Input.GetButtonDown("Fire1") && !hurt && !lockMovement)
		{
			animator.SetBool("Attack", true);
            Aim();
		}

        if (Input.GetButtonDown("Fire2") && !attacking && !hurt && !lockMovement)
        {
            currentColor = (PaintColor)(((int)currentColor + 1) % 3);
            GameUI.Singleton.SetIndicatorColor(currentColor);
           
            Material[] mats = modelMaterials.materials;
            mats[1] = colorMaterials[(int)currentColor];
            modelMaterials.materials = mats;

            Material[] mats2 = bucketMaterials.materials;
            mats2[1] = colorMaterials[(int)currentColor];
            bucketMaterials.materials = mats2;
        }

        onGround = Physics.Raycast(transform.position, Vector3.down, 1.2f, collisionMask);

        if (Input.GetButtonDown("Jump") && onGround && !attacking && !hurt && !lockMovement)
        {
            animator.SetBool("EndJump", false);
            rigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            animator.SetBool("StartJump", true);
            animator.SetBool("EndJump", false);
            animator.SetFloat("Jumping", 1);
        }

        if (!onGround)
        {
            if (rigidbody.velocity.y <= 0)
            {
                animator.SetFloat("Jumping", 2);
            }
            else
            {
                animator.SetFloat("Jumping", 1);
            }
        }
        else
        {
            animator.SetFloat("Jumping", 0);
        }			
		/*if (Input.GetButtonDown("Action"))a
		{
			Controller.Singleton.Dialogue(new List<string>(){"Hello there", "How are you today", "This is a test"});
		}*/
	}

    void Aim()
    {
        bool mouseAim = false;
        switch (currentColor)
        {
            case PaintColor.Red:
                mouseAim = redMouseAim;
                break;
            case PaintColor.Blue:
                mouseAim = blueMouseAim;
                break;
            case PaintColor.Yellow:
                mouseAim = yellowMouseAim;
                break;
        }
        if (mouseAim) 
        {
            MouseAim();
        }
        else
        {
            DirectionAim();
        }
    }

    void DirectionAim()
    {
        model.transform.rotation = modelRotation;
        aimRotation = modelRotation;
    }

    void MouseAim()
    {
        Vector3 aim = camera.transform.forward;
        if (aim.y<0 && onGround)
        {
            aim.y = 0;
        }
        aimRotation = Quaternion.LookRotation(aim);
        aim.y=0;
        modelRotation = Quaternion.LookRotation(aim);
        model.transform.rotation = modelRotation;
        horizontal = 0; vertical = 0;
    }


	void FixedUpdate()
	{
		Vector3 target = new Vector3(horizontal, 0f, vertical);
		target = camera.transform.TransformDirection(target);
		target.y = 0f;
		Vector3 result = target * Speed;
        
        if(hurt||dashing)
        {
            result = knockback;
        }
        else if (horizontal != 0 || vertical != 0)
        {
            modelRotation = Quaternion.LookRotation(target, Vector3.up);
        }

        if(!dashing)
        {
            result.y = rigidbody.velocity.y;
        }
        rigidbody.velocity = result;


        Quaternion newrot = Quaternion.Slerp(model.transform.rotation, modelRotation, RotationInterpolateSpeed * Time.deltaTime);
        model.transform.rotation = newrot;

        RotateCamera(mouseX, mouseY);
    }

    void RotateCamera(float movex, float movey)
    {
        cameraBase.transform.RotateAround(cameraBase.transform.position, new Vector3(0, 1, 0), movex * CameraMouseRotationSpeed);
        cameraXRot += -movey * CameraMouseRotationSpeed;
        cameraXRot = Mathf.Clamp(cameraXRot, CameraXRotMin, CameraXRotMax);
        Vector3 rot = cameraPivot.transform.rotation.eulerAngles;
        rot.x = cameraXRot;
        cameraPivot.transform.rotation = Quaternion.Euler(rot);
    }

    void Dash() 
    {
        animator.SetBool("Dash", true);
        model.transform.rotation = modelRotation;
        knockback = model.transform.forward * dashSpeed;
    }

    public void Attack()
    {
        Controller.Singleton.PlaySoundOneShot(paintSound, Random.Range(0.9f, 1.1f));
        GameObject obj;
        switch (currentColor)
        {
            case PaintColor.Red:
                obj = paintGlobsRed;
                break;
            case PaintColor.Blue:
                obj = paintGlobBlue;
                break;
            case PaintColor.Yellow:
                obj = paintGlobsYellow;
                break;
            default:
                obj = paintGlobsRed;
                break;
        }

        var glob = Instantiate(obj, transform.position + model.transform.forward * 1.5f, aimRotation);

        foreach (PaintGlob comp in glob.GetComponentsInChildren<PaintGlob>())
        {
            comp.Color = currentColor;
        }

        Destroy(glob.gameObject, 3.0f);
    }
}
