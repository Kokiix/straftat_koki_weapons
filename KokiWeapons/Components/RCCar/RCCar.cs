using FishNet;
using UnityEngine;

public class RCCar : MonoBehaviour
{
    private void Awake()
    {
        PauseManager.OnBeforeSpawn += Despawn;
    }

    private void Despawn()
    {
        if (InstanceFinder.IsServer && gameObject)
        {
            InstanceFinder.ServerManager.Despawn(gameObject);
        }
    }
}