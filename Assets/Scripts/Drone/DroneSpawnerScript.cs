using UnityEngine;
using System.Collections.Generic;

public class DroneSpawnerScript : MonoBehaviour
{
    [Header("Follow Target")]
    public GameObject followTarget;
    
    [Header("Drone Configuration")]
    public GameObject dronePrefab;

    public float spawnInterval = 5.0f;
    public float gracePeriod = 5f;
    public int maxDrones = 10;

    [Header("Spawn Area")]
    public float minSpawnDistance = 10.0f;

    [Header("Movement Settings")]
    public float minSpeed = 5.0f;
    public float maxSpeed = 10.0f;

    private List<GameObject> activeDrones = new List<GameObject>();
    private float spawnTimer;

    private Transform followTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        followTransform = followTarget.transform;
        spawnTimer = spawnInterval;
    }

    // Update is called once per frame
    void Update()
    {
        if(followTarget == null)
            return;
        
        activeDrones.RemoveAll(drone => drone == null);

        if(activeDrones.Count < maxDrones && spawnTimer <= 0)
        {
            GameObject drone = SpawnDrone();
            if(drone != null)
            {
                activeDrones.Add(drone);
            }

            spawnTimer = spawnInterval;
        }

        if(gracePeriod > 0)
        {
            gracePeriod -= Time.deltaTime;
        } else 
        {
            spawnTimer -= Time.deltaTime;
        }
    }

    private GameObject SpawnDrone()
    {
        if(dronePrefab == null || followTarget == null)
        {
            Debug.LogWarning("Drone Prefab or Follow Target is not set");
            return null;
        }

        Vector3 randomOffset = new Vector3(
            Random.Range(-minSpawnDistance, minSpawnDistance),
            Random.Range(0, minSpawnDistance),
            Random.Range(-minSpawnDistance, minSpawnDistance)
        );

        Vector3 spawnPosition = followTransform.position + randomOffset;

        GameObject drone = Instantiate(dronePrefab, spawnPosition, Quaternion.identity);
        drone.transform.parent = this.transform;

        DroneFollowScript droneMovement = drone.GetComponent<DroneFollowScript>();
        if(droneMovement != null)
        {
            droneMovement.player = followTarget;
            droneMovement.maxSpeed = 33f;
        }

        return drone;
    }
}
