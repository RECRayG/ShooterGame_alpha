using UnityEngine;

namespace Guns
{
    public interface IDamageable
    {
        public int CurrentHealth { get; }
        public int MaxHealth { get; }

        public delegate void TakeDamageEvent(int Damage);
        public event TakeDamageEvent OnTakeDamage;

        public delegate void DeathEvent(Vector3 Position, Vector3 playerPosition/*, Vector3 force, Vector3 hitPoint, float damageTaken*/);
        public event DeathEvent OnDeath;

        public void TakeDamage(int Damage, Vector3 playerPosition/*, Vector3 shootDirection, Vector3 hitPoint*/);
    }
}
