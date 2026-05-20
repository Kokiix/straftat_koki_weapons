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
    private float _decel;
    [SerializeField]
    private float _turnSpeed;
    [SerializeField]
    private float _maxSpeed;

    private FirstPersonController _driver;
    private UnityEngine.InputSystem.InputAction _moveInput;
    private bool _driving = false;

    private void Awake()
    {
        PauseManager.OnBeforeSpawn += Despawn;
    }

    private void Despawn()
    {
        Debug.LogError(InstanceFinder.NetworkManager);
        Debug.LogError(InstanceFinder.IsServer);
        Debug.LogError(gameObject);
        Debug.LogError(gameObject.GetComponent<NetworkObject>());
        Debug.LogError(gameObject.GetComponent<NetworkObject>().IsSpawned);
        if (InstanceFinder.NetworkManager && InstanceFinder.IsServer && gameObject && gameObject.GetComponent<NetworkObject>().IsSpawned)
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
        {
            if (_rb.velocity.magnitude < _maxSpeed)
                _rb.AddForce(transform.forward * _accel * inputVector.y);
        }
        else
            _rb.AddForce(transform.forward * _decel);
    }

    public void BeginDriving(FirstPersonController driver)
    {
        _moveInput = driver.move;
        _driving = true;
        _driver = driver;
        driver.sync___set_value_canMove(false, true);

        var cameraTransform = driver.playerCamera.transform;
        cameraTransform.SetParent(_cameraPosition);
        cameraTransform.localPosition = Vector3.zero;
        var bob = cameraTransform.Find("BobPosition");
        bob.Find("BothHandPositions").gameObject.SetActive(false);
        bob.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(false);

        driver.playerPickupScript.currentEnvironmentInteractable = new VictoryMenu();
    }

    public void EndDriving()
    {
        _driving = false;
        _driver.sync___set_value_canMove(true, true);
        if (!_driver.playerCamera) return;
        var cameraTransform = _driver.playerCamera.transform;
        cameraTransform.SetParent(_driver.playerCameraHolder.transform);
        var bob = cameraTransform.Find("BobPosition");
        bob.Find("BothHandPositions").gameObject.SetActive(true);
        bob.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(true);
    }
}