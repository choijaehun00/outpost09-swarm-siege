using UnityEngine;

public class addforce : MonoBehaviour
{
    public Rigidbody myRigidbody;
    void Start()
    {
        myRigidbody.AddForce(0, 500, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
