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
    private float _friction;
    [SerializeField]
    private float _turnSpeed;
    [SerializeField]
    private float _maxSpeed;
    [SerializeField]
    private float _driftMax;
    [SerializeField]
    private float _driftSpeed;

    private FirstPersonController _driver;
    private UnityEngine.InputSystem.InputAction _moveInput;

    public bool driving = false;

    private void Awake()
    {
        PauseManager.OnBeforeSpawn += Despawn;
    }

    private void Despawn()
    {
        if (InstanceFinder.NetworkManager && InstanceFinder.IsServer && gameObject && gameObject.GetComponent<NetworkObject>().IsSpawned)
        {
            InstanceFinder.ServerManager.Despawn(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (driving)
            EndDriving();
    }

    private void Update()
    {
        if (!driving) return;
        var inputVector = _moveInput.ReadValue<Vector2>();
        if (inputVector.x != 0)
        {
            transform.Rotate(0, inputVector.x * _turnSpeed * Time.deltaTime, 0);
            // Redirect velocity for turn
            var targetVelocity = transform.forward * _driftMax;
            _rb.velocity = Vector3.Lerp(
                _rb.velocity,
                targetVelocity,
                10 * _driftSpeed * Time.fixedDeltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!driving) return;
        var inputVector = _moveInput.ReadValue<Vector2>();

        // Gas/Brake
        if ((inputVector.y > 0 && _rb.velocity.magnitude < _maxSpeed) || inputVector.y < 0)
        {
            _rb.AddForce(transform.forward * _accel * inputVector.y);
        }

        // Forward friction
        if (Vector3.Dot(_rb.velocity, transform.forward) > 0)
            _rb.velocity -= transform.forward * _friction * -1;

    }

    public void BeginDriving(FirstPersonController driver)
    {
        _moveInput = driver.move;
        driving = true;
        _driver = driver;
        driver.sync___set_value_canMove(false, true);

        var cameraTransform = driver.playerCamera.transform;
        cameraTransform.SetParent(_cameraPosition);
        cameraTransform.localPosition = Vector3.zero;

        var arms = cameraTransform.Find("BobPosition").Find("FPArms");
        arms.Find("BothHandPositions").gameObject.SetActive(false);
        arms.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(false);
    }

    public void EndDriving()
    {
        driving = false;
        _driver.sync___set_value_canMove(true, true);
        if (!_driver.playerCamera) return;
        var cameraTransform = _driver.playerCamera.transform;
        cameraTransform.SetParent(_driver.playerCameraHolder.transform);

        var arms = cameraTransform.Find("BobPosition").Find("FPArms");
        arms.Find("BothHandPositions").gameObject.SetActive(true);
        arms.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(true);
    }
}