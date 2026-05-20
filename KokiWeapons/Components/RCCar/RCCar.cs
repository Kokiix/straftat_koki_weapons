using FishNet;
using FishNet.Object;
using UnityEngine;

public class RCCar : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraPosition;
    [SerializeField]
    private Rigidbody _rb;
    [SerializeField]
    private float _accel;
    [SerializeField]
    private float _turnSpeed;

    private FirstPersonController _driver;
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

    private void OnDestroy()
    {
        if (_driving)
            EndDriving();
    }

    private void Update()
    {
        if (!_driving) return;
        var inputVector = _moveInput.ReadValue<Vector2>();
        if (inputVector.x != 0)
            transform.Rotate(0, inputVector.x * _turnSpeed * Time.deltaTime, 0);
    }

    private void FixedUpdate()
    {
        if (!_driving) return;
        var inputVector = _moveInput.ReadValue<Vector2>();
        if (inputVector.y != 0)
            _rb.AddForce(transform.forward * _accel * inputVector.y);
    }

    public void BeginDriving(FirstPersonController driver)
    {
        _moveInput = driver.move;
        _driving = true;
        _driver = driver;
        driver.sync___set_value_canMove(false, true);

        driver.playerCamera.transform.SetParent(_cameraPosition);
    }

    public void EndDriving()
    {
        _driving = false;
        _driver.sync___set_value_canMove(true, true);
        _driver.playerCamera.transform.SetParent(_driver.playerCameraHolder.transform);
    }
}