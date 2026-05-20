using System;
using FishNet;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class RCCar : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraPosition;
    [SerializeField]
    private Rigidbody _rb;

    [Header("Movement Settings")]
    public float _accel;
    public float _maxSpeed;
    public float _turnSpeed;
    public AnimationCurve enginePowerCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [NonSerialized]
    public bool driving = false;
    private FirstPersonController _driver;
    private UnityEngine.InputSystem.InputAction _moveInput;

    private void Awake()
    {
        PauseManager.OnBeforeSpawn += Despawn;
    }

    private void Despawn()
    {
        if (InstanceFinder.NetworkManager && InstanceFinder.IsServer && gameObject != null && gameObject.GetComponent<NetworkObject>().IsSpawned)
        {
            InstanceFinder.ServerManager.Despawn(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (driving)
            EndDriving();
    }

    private Vector3 _inputVector;
    private void Update()
    {
        if (!driving) return;
        _inputVector = _moveInput.ReadValue<Vector2>();

        if (_inputVector.x != 0)
        {
            var newRotation = _inputVector.x * _inputVector.y * _turnSpeed * Time.deltaTime;
            transform.Rotate(0, newRotation, 0);
        }
    }

    private void FixedUpdate()
    {
        if (!driving) return;

        // Debug.LogError(_rb.velocity.magnitude);
        if (_inputVector.y != 0)
        {
            var speedRatio = Mathf.Clamp01(_rb.velocity.magnitude / _maxSpeed);
            var currEnginePower = enginePowerCurve.Evaluate(speedRatio);
            _rb.AddForce(transform.forward * _accel * currEnginePower * _inputVector.y, ForceMode.Acceleration);
        }
    }

    public void BeginDriving(FirstPersonController driver)
    {
        _moveInput = driver.move;
        driving = true;
        _driver = driver;

        // Freeze player
        driver.sync___set_value_canMove(false, true);

        // Move camera to car
        var cameraTransform = driver.playerCamera.transform;
        cameraTransform.SetParent(_cameraPosition);
        cameraTransform.localPosition = Vector3.zero;

        // Disable arms
        var arms = cameraTransform.Find("BobPosition").Find("FPArms");
        arms.Find("BothHandPositions").gameObject.SetActive(false);
        arms.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(false);

        // Set up EndDriving
        var dropAction = InputManager.inputActions.Player.Drop;
        dropAction.performed -= driver.playerPickupScript.HandleDrop;
        dropAction.performed += EndDriving;
    }

    public void EndDriving(InputAction.CallbackContext context)
    {
        EndDriving();
        var dropAction = InputManager.inputActions.Player.Drop;
        dropAction.performed -= EndDriving;
        dropAction.performed += _driver.playerPickupScript.HandleDrop;
        _driver.playerPickupScript.HandleDrop(context);
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