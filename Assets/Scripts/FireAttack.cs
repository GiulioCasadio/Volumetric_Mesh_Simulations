using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireAttack : MonoBehaviour
{
    public GameObject bulletPrefab;
    public int bulletSpeed;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation) as GameObject;
            bullet.GetComponent<Rigidbody>().mass = 1;
            bullet.GetComponent<Rigidbody>().AddForce(transform.forward*bulletSpeed);
        }
    }
}
