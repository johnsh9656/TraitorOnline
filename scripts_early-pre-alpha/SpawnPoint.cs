using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private Transform[] spawnpoints;

    public Transform GetSpawnpoint(int index)
    {
        return spawnpoints[index];
    }
}
