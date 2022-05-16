using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGun : MonoBehaviour
{
    public GameObject LineRendererPrefab;
    public Transform BarrelEnd;
    float RateOfFire = 5f;
    float LastFire;

    void Update()
    {
        if (PlayerMovement.PlayerInput.Player.Fire.ReadValue<float>() > 0 && Time.time > LastFire + 1 / RateOfFire)
        {
            LastFire = Time.time;
            Ray ray = new Ray(PlayerMovement.Player.HeadCamera.position, PlayerMovement.Player.HeadCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100))
            {
                GameObject line = Instantiate(LineRendererPrefab);
                LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                lineRenderer.SetPosition(0, BarrelEnd.position);
                lineRenderer.SetPosition(1, hit.point);
                Destroy(line, 10);
            }
        }
    }
}
