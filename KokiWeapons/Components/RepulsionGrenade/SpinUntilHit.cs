using UnityEngine;

public class SpinUntilHit : MonoBehaviour
{
    private bool _hasHitGround = false;
    public Vector3 axis;
    public float rotateSpeed;
    public float collisionRadius;

    public void FixedUpdate()
    {
        if (!_hasHitGround)
        {
            if (Physics.CheckSphere(transform.position, collisionRadius, 1 << 0))
            {
                transform.localRotation = Quaternion.identity;
                _hasHitGround = true;
            }
            else transform.Rotate(axis * rotateSpeed * Time.deltaTime);
        }
    }
}