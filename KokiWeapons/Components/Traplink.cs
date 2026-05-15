using UnityEngine;

public class TPTrap : MonoBehaviour
{
    public GameObject otherTrap;
    public bool clientDetonated;

    public void Awake()
    {
        var anim = transform.Find("TeleTrapPhysMesh").Find("trap_010").GetComponent<Animation>();
        anim["tpmineSphere"].layer = 0;
        anim.Play("tpmineSphere");
        anim["tpmineTorus"].layer = 1;
        anim.Play("tpmineTorus");
    }
}