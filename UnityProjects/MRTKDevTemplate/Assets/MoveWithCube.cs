using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MoveWithCube : MonoBehaviour
{
    public GameObject mover;
    float offsetZ = -0.4180163f - -0.4067162f;
    // Start is called before the first frame update
    void Start()
    {
        //Vector3 offset = mover.transform.position - this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = new Vector3(mover.transform.position.x, mover.transform.position.y, mover.transform.position.z + offsetZ);
        this.transform.rotation = mover.transform.rotation;
    }
}
