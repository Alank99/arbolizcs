using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerController : MonoBehaviour
{
    [Header("Referencias")]
    Rigidbody2D playerRB;
    public Transform playerSprites;
    public Animator playerAnim;

    [Header("Movimiento lateral")]

    public float maxSpeedX;

    public float airtimeControlReduction;
    public Vector2 sensitivity;
    public Vector2 initialPushWhenGrounded;

    public float spriteScale;

    public float healthAmount = 100f;

    [Header("Cosas para el brinco")]

    /// <summary>
    /// Explica la velocidad que se le aplica al jugador después de presionar brincar
    /// </summary>
    public float jumpForce;
    /// <summary>
    /// La gravedad que se le va a aplicar cuando precione espacio
    /// </summary>
    public float initialGravity;
    /// <summary>
    /// La gravedad el resto del tiempo
    /// </summary>
    public float finalGravity;
    /// <summary>
    /// Cual es el tiempo máximo que el jugador puede brincar 
    /// </summary>
    public float maxJumpTime;

    public AnimationCurve jumpCurve;

    [Header("Cosas para el dash")]
    [Tooltip("If the player has unlocked the dash ability")]
    public int has_dash;
    public float dashTime;
    public Vector2 dashForce;
    //If the player currently can dash
    private int hasDash = 1;
    //1=left, 0=right
    private int moving_left = 0;

    [Header("Estadísticas del sistema")]

    public bool grounded;
    public bool jumping;
    public float elapsed;
    public Vector2 movement;

    private void Start() {
        playerRB = gameObject.GetComponent<Rigidbody2D>();
        grounded = true;
        if(saveload.savedgame){
            loadGame();
        }
    }

    public void TouchGrass(){
        grounded = true;
        playerAnim.SetTrigger("fall");
        stopJump();
    }
    public void StopTouchGrass(){
        grounded = false;
        //stopJump();
    }

    private void Update() {
        var cacheSens = grounded ? sensitivity : sensitivity * airtimeControlReduction;
        playerRB.AddForce(new Vector2(movement.x * cacheSens.x * Time.deltaTime, 
                                      movement.y * cacheSens.y * Time.deltaTime));

        if (playerRB.velocity.x >  maxSpeedX){
            playerRB.velocity = new Vector2(maxSpeedX, playerRB.velocity.y);
            playerSprites.localScale = new Vector3(-spriteScale,spriteScale,spriteScale);
        }

        if (playerRB.velocity.x <  -maxSpeedX){
            playerRB.velocity = new Vector2(-maxSpeedX, playerRB.velocity.y);
            playerSprites.localScale = new Vector3(spriteScale,spriteScale,spriteScale);
        }
        if (grounded && hasDash == 0) hasDash = 1;

        if (Input.GetKeyDown(KeyCode.P)) loadGame();
        
        if (Input.GetKeyDown(KeyCode.O)) saveGame();        
    }

    //guardar partida
    //a modificar los punto de vida?
    public void saveGame(){
        saveload.player_estatus.position = transform.position;
        saveload.player_estatus.health_p = 100;
        saveload.savedgame = true;
    }

    //cargar partida
    public void loadGame(){
        transform.position = saveload.player_estatus.position;
        double Hp=saveload.player_estatus.health_p;
    }

    // no puedes usar un collision 2d!!! necesitas tener un collider2d para un trigger. El collider es cuando no es trigger
    // private void OnTriggerEnter2D(Collision2D col) {
    //     if(col.gameObject.tag == "cheackpoint"){
    //         saveGame();
    //     }
    // }

    private void OnTriggerEnter2D(Collider2D col) {
        if(col.gameObject.tag == "cheackpoint"){ // Se escribe checkpoint
            saveGame();
        }
    }

    /// <summary>
    /// Se ejecuta cuando se presiona el botón de Dodge y se regenera al tocar el piso
    /// </summary>
    /// <param name="value"></param>
    public void OnDoge(){
        if (has_dash == 0) return;
        Vector2 force = new Vector2(0, 0);
        //Checa si toco el piso antes del dash
        if (hasDash == 1){
            if (moving_left == 0 && playerRB.velocity.x != 0)
                force.x = dashForce.x;
            else if (moving_left == 1 && playerRB.velocity.x != 0)
                force.x = -dashForce.x;

            //Checa si hay algo en la direccion de la fuerza
            RaycastHit2D hit = Physics2D.Raycast(transform.position, force, Mathf.Abs(force.x), LayerMask.GetMask("Ground"));
            if (hit.collider!= null)
                StartCoroutine(MoveFunction(hit.point));
            else
                StartCoroutine(MoveFunction(playerRB.position + force * Time.deltaTime)); 

            hasDash = 0;
        }
    }

    IEnumerator MoveFunction(Vector2 newPosition)
    {
        float timeSinceStarted = 0f;
        while (true)
        {
            timeSinceStarted += Time.deltaTime;
            playerRB.MovePosition(Vector3.Lerp(playerRB.position, newPosition, timeSinceStarted));

            // If the object has arrived, stop the coroutine
            if ((Vector3.Distance(playerRB.position, newPosition) < 1f) || timeSinceStarted > 1f)
            {
                yield break;
            }

            // Otherwise, continue next frame
            yield return null;
        }
    }

    /// <summary>
    /// Utilizado por el player controller, regresa que tanto esta movido algo
    /// </summary>
    /// <param name="value"></param>
    public void OnMove(InputValue value){
        movement = value.Get<Vector2>();

        if (movement.x > 0)
            moving_left = 0;
        else if (movement.x < 0)
            moving_left = 1;

        if (movement.x == 0){
            playerRB.velocity = new Vector2(playerRB.velocity.x/2, playerRB.velocity.y);
            playerAnim.SetBool("walkLeft", false);
        }
        else {
            playerAnim.SetBool("walkLeft", true);
        }

        if (grounded){
            playerRB.velocity = new Vector2(movement.x * initialPushWhenGrounded.x, playerRB.velocity.y);
        }
    }

    /// <summary>
    /// Esta madre se corre asincrona. Se inicia con startCoroutine y termina cuando se sale o stopcoroutine
    /// </summary>
    /// <returns></returns>
    IEnumerator jumpController(){
        jumping = true;
        playerAnim.SetTrigger("jump");
        // la x se mantiene para que no interferimos con ella
        playerRB.velocity = new Vector2(playerRB.velocity.x, jumpForce);
        playerRB.gravityScale = initialGravity;

        // Cálculos generales de tiempo
        var startTime = Time.time;
        elapsed = Time.time - startTime;
        // relga de 3 para que sepamos que porcentaje llevamos (va de 0 a 1)
        var percentageElapsed = elapsed/maxJumpTime;

        var localGravityScale = initialGravity;

        while (elapsed < maxJumpTime){
            elapsed = Time.time - startTime;
            percentageElapsed = elapsed/maxJumpTime;

            // vamos a hacer una interpolación lineal de donde estamos, a donde debemos de estar
            localGravityScale = Mathf.Lerp(initialGravity, finalGravity, jumpCurve.Evaluate(percentageElapsed)); // el jump curve es para conseguir el y
            playerRB.gravityScale = localGravityScale;

            // Aquí esperamos al siguiente calculo de física
            yield return new WaitForFixedUpdate();
        }

        playerRB.gravityScale = initialGravity;
        jumping = false;
    }


    /// <summary>
    /// Termina la corutina y baja al personaje
    /// </summary>
    private void stopJump(){
        StopCoroutine("jumpController");
        playerAnim.ResetTrigger("jump");
        playerRB.gravityScale = finalGravity;
    }

    /// <summary>
    /// Se corre cuando se preciona espacio y cuando se termina de precionar
    /// </summary>
    /// <param name="state"></param>
    public void OnJump(InputValue state){
        if (state.Get<float>() > 0.5f){ // diferencia entre preciona y deja de
            // Nota: este es cuando se inicia el brinco
            if (grounded)
                StartCoroutine("jumpController");
        }
        else{
            // Aquí es cuando se termina el brinco
            stopJump();
        }
    }
    
    /// <summary>
    /// gets the current mouse pos, and returns the vector from the point of reference to the mouse
    /// </summary>
    /// <param name="pointOfReference">The point that is being looked at</param>
    /// <returns></returns>
    public static Vector2 mousePosVector(Vector2 pointOfReference){
        return (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - pointOfReference;
    }
}
