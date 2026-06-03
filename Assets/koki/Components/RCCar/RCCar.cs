using FishNet;
using FishNet.Object;
using UnityEngine;

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
    [Header("Movement Settings")]
    public float accel;
    public float maxSpeed;
    public float turnSpeed;
    public AnimationCurve enginePowerCurve = AnimationCurve.Linear(0, 1, 1, 0);
}