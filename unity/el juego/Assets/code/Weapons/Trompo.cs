using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trompo : MonoBehaviour
{
    [SerializeField] GameObject png;
    [Header("Spin y Daño del trompo")]
    [Tooltip("Cuanto giro pierde el trompo cuando un enemigo entra en contacto con él")]
    [SerializeField] float SpinLoss;
    [Tooltip("Multiplicador de daño la velocidad del trompo con su giro")]
    [SerializeField] float SpinMult;
    [Tooltip("Tiempo que tarda en reducirse el giro del trompo")]
    [SerializeField] float slowdown;


    private float SpinSpeed;
    private float time_since_slow;

    // Start is called before the first frame update
    void Start()
    {
        time_since_slow = Time.unscaledTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.unscaledTime - time_since_slow >= slowdown)
        {
            time_since_slow = Time.unscaledTime;
            SpinSpeed -= 1;
        }
        if (SpinSpeed <= 0)
        {
            this.GetComponent<Rigidbody2D>().freezeRotation = false;
            png.GetComponent<Animator>().SetBool("Spin", false);
        }
    }

    //Colisiones con enemigos
    void OnCollisionEnter2D(Collision2D col)
    {
        if (SpinSpeed > 0 && col.gameObject.tag == "Enemy")
        {
            SpinSpeed -= SpinLoss;
            col.gameObject.GetComponent<Enemigos>().addDamage((int)SpinSpeed);
            //this.GetComponent<CircleCollider2D>().enabled = false;
        }
    }

    //Velocidad inicial del trompo
    public void setSpinSpeed(float speed)
    {
        SpinSpeed = speed * SpinMult;
    }
}