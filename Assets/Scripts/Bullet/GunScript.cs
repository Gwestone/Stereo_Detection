using UnityEngine;

public class GunScript : MonoBehaviour
{

    public GameObject bulletPrefab;
    private int bulletConter = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //bulletPrefab.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        bulletConter++;
        if (bulletPrefab != null && bulletConter == 10)
        {
            bulletConter = 0;
            //turret is rotated by 90 degrees to account the correct rotation for gimbals
            Instantiate(bulletPrefab, transform.position + transform.right * 1.5f, transform.rotation * Quaternion.Euler(0, 90, 0));
        }
    }
}
