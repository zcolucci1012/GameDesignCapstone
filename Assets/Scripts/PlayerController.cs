using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private Rigidbody rb;
    [SerializeField] public float speed = 5;
    [SerializeField] private float turnSpeed = 360;
   // [SerializeField] private GameObject player;
    private Vector3 _input;
    // Start is called before the first frame update
    void Start()
    {
     //   player = GameObject.FindGameObjectWithTag("Player");
      //  rb = player.GetComponent<Rigidbody>();
    }

    void gatherInput()
    {
        _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
    }

    void move()
    {
        rb.MovePosition(transform.position + (transform.forward * _input.magnitude) * speed * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        gatherInput();
        look();
    }

    private void FixedUpdate()
    {
        move();
    }

    void look()
    {

        if(_input != Vector3.zero)
        {
            var matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));

            var skewedInput = matrix.MultiplyPoint3x4(_input);
            var relative = (transform.position + skewedInput) - transform.position;
            var rot = Quaternion.LookRotation(relative, Vector2.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * Time.deltaTime);
        }

    }
}
