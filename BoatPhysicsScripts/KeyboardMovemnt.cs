using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardMovemnt : MonoBehaviour
{
    public float speed= 10f;
    public Transform jetTransfrom;

    private Rigidbody boatRB;
    // Start is called before the first frame update
    void Start()
    {
        boatRB = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.W))
        {
            Vector3 newSpeed = new Vector3(0f, 0f, speed);
            boatRB.AddForceAtPosition(newSpeed, boatRB.position, ForceMode.Acceleration);
        }
    }
}
