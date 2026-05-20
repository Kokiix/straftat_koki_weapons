using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using HarmonyLib;
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
    [Header("Bomb Car")]
    public GameObject explosionVFX;
    public AudioClip explosionSound;
    public GameObject explosionDecal;
    public GameObject bloodDecal;
    public float explosionRadius;
    private bool _exploded = false;
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
        if (this && gameObject)
        {
            UnityEngine.Object.Destroy(gameObject);
            if (InstanceFinder.IsServer && gameObject.GetComponent<NetworkObject>().IsSpawned)
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
        driver.playerCamera = null;

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
        var input = InputManager.inputActions.Player;
        input.Interact.performed -= driver.playerPickupScript.HandleInteraction;
        input.Interact.performed += EndDriving;
        input.FireHold.performed += TriggerWeapon;
    }

    public void TriggerWeapon(InputAction.CallbackContext _)
    {
        if (carType == CarType.Boom && !_exploded)
        {
            _exploded = true;
            if (driving)
            {
                EndDriving();
                _driver.playerPickupScript.objInHand.GetComponent<WeaponHandSpawner>().currentAmmo = 0;
            }
            Explode();
        }
    }

    public void EndDriving(InputAction.CallbackContext context)
    {
        EndDriving();
        var input = InputManager.inputActions.Player;
        input.Interact.performed += _driver.playerPickupScript.HandleInteraction;
        input.Interact.performed -= EndDriving;
        input.FireHold.performed -= TriggerWeapon;
    }

    public void EndDriving()
    {
        driving = false;

        if (_playerGraphicsTransform)
        {
            foreach (Transform transform in _playerGraphicsTransform)
            {
                if (!transform) return;
                transform.gameObject.SetActive(false);
            }
            _playerGraphicsTransform.GetChild(6).gameObject.SetActive(true); // Hips needed for collision
        }

        if (!_cameraTransform) return;
        _driver.playerCamera = _cameraTransform.GetComponent<Camera>();
        _cameraTransform.SetParent(_driver.playerCameraHolder.transform);
        _cameraTransform.localPosition = Vector3.zero;

        var arms = _cameraTransform.Find("BobPosition").Find("FPArms");
        arms.Find("BothHandPositions").gameObject.SetActive(true);
        arms.Find("PF_FPArm_Container_IK_00").gameObject.SetActive(true);
    }

    // Mostly ripped from PhysicsGrenade.HandleExplosion
    private void Explode()
    {
        Physics.OverlapSphere(transform.position, explosionRadius, 1 << 11 | 1 << 16)
        .Where(collider =>
        {
            if (collider.transform.tag == "ShatterableGlass" && collider.gameObject.GetComponent<ShatterableGlass>())
                collider.gameObject.GetComponent<ShatterableGlass>().Shatter3D(collider.transform.position, collider.transform.position - base.transform.position);
            // Debug.LogError(collider.name);
            return collider.GetComponentInParent<PlayerHealth>();
        })
        .Select(collider => collider.GetComponentInParent<PlayerHealth>())
        .Distinct()
        .Do(health =>
        {
            // Debug.LogError(health);
            if (!InstanceFinder.IsServer || !health || health.sync___get_value_isKilled())
                return;

            UnityEngine.Object.Instantiate(bloodDecal, health.transform.position, Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f));

            health.ChangeKilledState(tempBool: true);
            health.RemoveHealth(10f);
            if (health.transform.gameObject == _driver.transform.gameObject)
            {
                Settings.Instance.IncreaseSuicidesAmount();
                health.suicide = true;
            }
            else
            {
                // KillShockWave();
                PauseManager.Instance.WriteLog("<b><color=#" + PauseManager.Instance.selfNameLogColor + ">" + health.sync___get_value_playerValues().playerClient.PlayerNameTag + "</color></b> was blown up using a <b><color=white>RC Car</color></b> by <b><color=#" + PauseManager.Instance.enemyNameLogColor + ">" + ClientInstance.Instance.PlayerNameTag + "</color></b>");
            }
            health.Explode(
                explode: false,
                dismemberment: true,
                health.gameObject.name,
                health.transform.position - base.transform.position,
                force: 60,
                base.transform.position);
            health.SetKiller(_driver.transform);
        });
        UnityEngine.Object.Instantiate(explosionVFX, transform.position, Quaternion.identity);
        SoundManager.Instance.PlaySound(explosionSound);
        Despawn();
    }
}