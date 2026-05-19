using FishNet;
using FishNet.Object;
using UnityEngine;

public class RCCar : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _rb;

    private UnityEngine.InputSystem.InputAction _moveInput;
    private bool _driving = false;

    private void Awake()
    {
        PauseManager.OnBeforeSpawn += Despawn;
    }

    private void Despawn()
    {
        if (InstanceFinder.IsServer && gameObject.GetComponent<NetworkObject>().IsSpawned)
        {
            InstanceFinder.ServerManager.Despawn(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (!_driving) return;
        var inputVector = _moveInput.ReadValue<Vector2>();
        Debug.LogError(inputVector.y);
        // if (_moveInput.)
    }

    public void BeginDriving(FirstPersonController driver)
    {
        _moveInput = driver.move;
        _driving = true;
        driver.sync___set_value_canMove(false, true);
    }
}