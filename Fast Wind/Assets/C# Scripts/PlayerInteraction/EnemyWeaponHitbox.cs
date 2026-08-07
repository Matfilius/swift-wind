using UnityEngine;

public class EnemyWeaponHitbox : MonoBehaviour
{
    [SerializeField] private Attack attack;
    private void Awake()
    {
        if (attack == null)
            attack = GetComponentInParent<Attack>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        attack.TryHitPlayer(other);
    }
}
