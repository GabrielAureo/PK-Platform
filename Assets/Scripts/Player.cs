﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Player : MonoBehaviour
{   
    enum State{ Normal, Casting, Grabbing }
    [SerializeField] float walkSpeed;
    [SerializeField] float maxJumpHeight;
    [SerializeField] float minJumpHeight;
    [SerializeField] float slowGravityScale;

    [SerializeField] float throwDuration;

    [SerializeField] GameObject freezeProjectile;
    [SerializeField] AudioClip rewindSFX;
    [SerializeField] Transform grabSocket;
    [SerializeField] Transform projectileSpawn;

    bool canJump = true;
    bool releasedJump = false;
    bool jumping = false;
    float jumpForce;
    float defaultGravityScale;

    State state;
    Rewindable lastRewindable;
    Grabbable grabbing;
    

    Rigidbody2D rb;
    Animator anim;
    AudioSource source;
    

    void OnValidate(){
        if(rb== null) rb = GetComponent<Rigidbody2D>();
        jumpForce = Mathf.Sqrt(2f * Physics2D.gravity.magnitude * rb.gravityScale * maxJumpHeight) * rb.mass;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        source = GetComponent<AudioSource>();
        anim = GetComponentInChildren<Animator>();
        defaultGravityScale = rb.gravityScale;
        jumpForce = Mathf.Sqrt(2f * Physics2D.gravity.magnitude * rb.gravityScale * maxJumpHeight) * rb.mass;
    }
    #region Basic Actions
    public void Jump(){
        jumping = true;
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);
        canJump = false;
    }
    void Walk(float direction){
        rb.velocity = new Vector2(walkSpeed * direction, rb.velocity.y);
        anim.SetFloat
        ("speed", Mathf.Abs(rb.velocity.x));
    }

    void Movement(){
        if(Input.GetKeyDown(KeyCode.Space) && canJump){
            releasedJump = false;
            Jump();
        }
        releasedJump = Input.GetKeyUp(KeyCode.Space);
        if(jumping && releasedJump){
           //StartCoroutine(WaitMinimumJump());
        }
        var hor = Input.GetAxisRaw("Horizontal");
        if(hor != 0){
            var scale = transform.localScale;
            transform.localScale = new Vector3 (hor * Mathf.Abs(scale.x) ,scale.y,scale.z);
        }       
        Walk(hor);
    }

    void LaunchProjectile(){

        var projectile = Instantiate(freezeProjectile,projectileSpawn.position, transform.rotation);
        GameManager.instance.cameraController.SetTarget(projectile.transform, true);

        var projScript = projectile.GetComponent<GuidedProjectile>();

        projScript.Initialize(CheckRewindable, ReturnControl, KeyCode.E);

        rb.velocity = Vector2.zero;
        rb.gravityScale = slowGravityScale;
        state = State.Casting;
    }
    #endregion

    void Grab(Grabbable grabbable){
        grabbable.Grab(grabSocket);
        grabbing = grabbable;
        grabbing.onRelease.AddListener(()=>grabbing = null);
        grabbing.onThrow.AddListener(()=>grabbing = null);
        state = State.Grabbing;

    }
    void Throw(){
        grabbing.Throw(maxJumpHeight - 2.5f, 4);
        state = State.Normal;        
    }

    void Rewind(){
        if(lastRewindable != null){
            if(!lastRewindable.isActive()){
                lastRewindable = null;
                return;
            }
            source.PlayOneShot(rewindSFX);
            lastRewindable.Rewind();
        }
        
    }

    void Attack(){
        if(state != State.Grabbing){
            if(Input.GetKeyDown(KeyCode.E)){
                LaunchProjectile();
            }
            if(Input.GetKeyDown(KeyCode.R)){
                Rewind();
            }
        }

        if(Input.GetKeyDown(KeyCode.LeftShift)){
            if(grabbing != null){
                Throw();
            }else{
                CheckForGrabbables(Vector2.down);
            }
            
        }
    }

    #region Utility Methods

    public void AllowJump(){
        canJump = true;
    }
    void CheckBellow(Collision2D coll){
        //collision from bellow
        if(Vector3.Dot(coll.GetContact(0).normal, Vector3.up) > 0.5){
            if(coll.gameObject.tag == "Ground" && coll.enabled){
                canJump = true;
                jumping = false;
            }else{
                var stompable = coll.gameObject.GetComponent<Stompable>();
                if(stompable != null){
                    Jump();
                    stompable.Stomp();
                }
            }
        }
    }
    void CheckRewindable(MonoBehaviour effect){
        if(effect is Rewindable){
            this.lastRewindable = (Rewindable) effect;
        }
    }

    void ReturnControl(){
        state = State.Normal;
        rb.gravityScale = defaultGravityScale;
        GameManager.instance.cameraController.ReturnToPlayer();
    }

    void CheckForGrabbables(Vector2 direction){
        int notPlayerLayer = ~(1 << 8);
        var hit = Physics2D.Raycast(transform.position,transform.TransformDirection(direction),2f, notPlayerLayer);
        if(hit.collider != null){
            var grabbable = hit.transform.GetComponent<Grabbable>();
            if(grabbable){
                Grab(grabbable);
            }
        }
    }
    #endregion

    void Update()
    {
        if(state != State.Casting){
            Movement();
            Attack();
        }
        
    }

    void OnCollisionEnter2D(Collision2D coll){
        CheckBellow(coll);   
    }

    void OnCollisionStay2D(Collision2D coll){
        //Checks if the player has jumped and cast magic at the same time
        if(!canJump && state == State.Casting){
            CheckBellow(coll);
        }
    }

    void OnDrawGizmosSelected(){
        Gizmos.color = Color.red;
        Gizmos.DrawRay(new Ray(transform.position, transform.TransformDirection(Vector2.right) * 2f));
        Gizmos.DrawRay(new Ray(transform.position, -transform.up * 2f));
    }


    

}
