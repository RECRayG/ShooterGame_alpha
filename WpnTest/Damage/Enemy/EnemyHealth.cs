using NTC.Global.Pool;
using UnityEngine;
using UnityEngine.UIElements;

namespace Guns.Enemy
{
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private int _Health;
        [SerializeField]
        private int _MaxHealth = 100;
        [SerializeField]
        public bool isHead = false;
        [SerializeField]
        public ProgressBar HeadHealthBar;
        [SerializeField]
        public ProgressBar MainHealthBar;
        public int CurrentHealth { get => _Health; set => _Health = value; }
        public int MaxHealth { get => _MaxHealth; private set => _MaxHealth = value; }

        public event IDamageable.TakeDamageEvent OnTakeDamage;
        public event IDamageable.DeathEvent OnDeath;

        private void OnEnable()
        {
            _Health = MaxHealth;
        }

        public void TakeDamage(int Damage, Vector3 playerPosiition/*, Vector3 shootDirection, Vector3 hitPoint*/)
        {
            int damageTaken = Mathf.Clamp(Damage, 0, CurrentHealth);

            CurrentHealth -= damageTaken;

            if (damageTaken != 0)
            {
                OnTakeDamage?.Invoke(damageTaken);
            }

            if (CurrentHealth <= 0 && damageTaken != 0)
            {
                /*float forcePercent = damageTaken / _MaxHealth;
                float forceMagnitude = Mathf.Lerp(1, damageTaken, forcePercent);*/

                // –ассчитываем вектор направлени€ удара
                //Vector3 direction = shootDirection.normalized;

                OnDeath?.Invoke(transform.position, playerPosiition/*, direction, hitPoint, damageTaken*/);

                if (MainHealthBar != null)
                    MainHealthBar.gameObject.SetActive(false);//NightPool.Despawn(MainHealthBar);

                if (HeadHealthBar != null)
                    HeadHealthBar.gameObject.SetActive(false);//NightPool.Despawn(HeadHealthBar);
            }
        }
    }
}