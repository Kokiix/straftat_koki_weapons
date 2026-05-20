using System;
using FishNet;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CarType
{
    Boom
}

public class RCCar : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraPosition;
    [SerializeField]
    private Rigidbody _rb;

    public CarType carType;
    [Header("Movement Settings")]
    public float accel;
    public float maxSpeed;
    public float turnSpeed;
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
        if (this && InstanceFinder.IsServer && gameObject && gameObject.GetComponent<NetworkObject>().IsSpawned)
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
            var newRotation = _inputVector.x * _inputVector.y * turnSpeed * Time.deltaTime;
            transform.Rotate(0, newRotation, 0);
        }
    }

    private void FixedUpdate()
    {
        if (!driving) return;

        // Debug.LogError(_rb.velocity.magnitude);
        if (_inputVector.y != 0)
        {
            var speedRatio = Mathf.Clamp01(_rb.velocity.magnitude / maxSpeed);
            var currEnginePower = enginePowerCurve.Evaluate(speedRatio);
            _rb.AddForce(transform.forward * accel * currEnginePower * _inputVector.y, ForceMode.Acceleration);
        }
    }


    private Transform _cameraTransform;
    private Transform _playerGraphicsTransform;
    public void BeginDriving(FirstPersonController driver)
    {
        _moveInput = driver.move;
        driving = true;
        _driver = driver;

        // Freeze player
        driver.sync___set_value_canMove(false, true);

        // Move camera to car
        _cameraTransform = driver.playerCamera.transform;
        _cameraTransform.SetParent(_cameraPosition);
        _cameraTransform.localPosition = Vector3.zero;

        // Enable player to see self
        _playerGraphicsTransform = driver.transform.root.Find("Graphics").Find("PF_Aboubi_04").Find("SK_Aboubi_00");
        foreach (Transform transform in _playerGraphicsTransform)
        {
            transform.gameObject.SetActive(true);
        }

        // Disable arms
        var arms = _cameraTransform.Find("BobPosition").Find("FPArms");
        arms.Find("BothHandPositions").gameObject.SetActive(false);
        arms.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(false);

        // Set up EndDriving
        var dropAction = InputManager.inputActions.Player.Interact;
        dropAction.performed -= driver.playerPickupScript.HandleInteraction;
        dropAction.performed += EndDriving;
    }

    public void TriggerWeapon()
    {

    }

    public void EndDriving(InputAction.CallbackContext context)
    {
        EndDriving();
        var dropAction = InputManager.inputActions.Player.Interact;
        dropAction.performed -= EndDriving;
        dropAction.performed += _driver.playerPickupScript.HandleInteraction;
    }

    public void EndDriving()
    {
        driving = false;
        _driver.sync___set_value_canMove(true, true);

        foreach (Transform transform in _playerGraphicsTransform)
        {
            transform.gameObject.SetActive(false);
        }

        _cameraTransform.SetParent(_driver.playerCameraHolder.transform);
        _cameraTransform.localPosition = Vector3.zero;

        var arms = _cameraTransform.Find("BobPosition").Find("FPArms");
        arms.Find("BothHandPositions").gameObject.SetActive(true);
        arms.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(true);
    }
}