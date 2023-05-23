using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class genericMonster : MonoBehaviour
{
    /// <summary>
    /// the position where the monster will try to go to
    /// </summary>
    protected Vector3 targetPos;

    /// <summary>
    /// monster Rigidbody (to add forces)
    /// </summary>
    protected Rigidbody2D rb;

    [Header("MacMovement")]
    public float maxSpeedX;
    public float Force;
    public float jumpForce;
    public Vector2 waitTime;

    bool alive = true;
    /// <summary>
    /// Define si el mounstro se encuentra activo o no. 
    /// </summary>
    /// <value></value>
    public bool active { get; private set; } = false;

    public int health { get; private set; }

    public MonsterTargetingType monsterTargetMethod;

    // get rb reference from self on start
    protected void StartMonster() {
        active = true;
        rb = gameObject.GetComponent<Rigidbody2D>();

        monsterHasActivated();

        switch (monsterTargetMethod)
        {
            case MonsterTargetingType.jumps:
                StartCoroutine("randomJumps");
                break;
            case MonsterTargetingType.walk:
                StartCoroutine("randomWalk");
                break;
            case MonsterTargetingType.fly:
                StartCoroutine("randomFly");
                break;
        }
    }

    IEnumerator randomJumps(){
        while (alive){
            // if (Vector3.Magnitude(transform.position - targetPos) < 3f){
            //     Debug.Log("Changind dir");
            //     currentLock = currentLock == pos1 ? pos2 : pos1;
            // }

            var moveTowards = Vector3.MoveTowards(transform.position, targetPos, maxSpeedX) - transform.position;
            rb.velocity =  new Vector2(moveTowards.x * Force, jumpForce);
            
            yield return new WaitForSeconds(Random.Range(waitTime.x, waitTime.y));
        }
    }

    private void killSelf(){
        alive = false;
        Destroy(this);
        Debug.Log("I'M DEAD");
    }

    public void takeDamage(int damage){
        health -= damage;

        if (health <= 0){
            killSelf();
        }
    }

    private void OnCollisionEnter2D(Collision2D other) {
        Debug.Log($"Llegamos aca {other.collider.tag}");
        if (other.collider.tag == "PlayerCollider"){
            giveDamage(other.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "PlayerCollider"){
            giveDamage(other.gameObject);
        }

        if (other.tag == "PlayerRadius"){
            StartMonster();
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.tag == "PlayerRadius"){
            monsterHasDeactivated();
            StopAllCoroutines();
            active = false;
        }
    }

    public abstract void giveDamage(GameObject player);
    public abstract void monsterHasActivated();
    public abstract void monsterHasDeactivated();
}


public enum MonsterTargetingType{
    jumps = 0,
    walk = 1,
    fly = 2
}