using UnityEngine;
using UnityEngine.Pool;

namespace ImpactSystem.Pool
{
    public class PoolableObject : MonoBehaviour
    {
        public ObjectPool<GameObject> Parent;

        private void OnDisable()
        {
            if (Parent != null)
            {
                Parent.Release(gameObject);
            }
        }
    }
}