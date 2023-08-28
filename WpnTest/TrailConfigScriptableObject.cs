using UnityEngine;

namespace Guns
{
    // То, как показывать след от пуль
    [CreateAssetMenu(fileName = "Trail Config", menuName = "Guns/Trail Config", order = 4)]
    public class TrailConfigScriptableObject : ScriptableObject
    {
        public Material Material; // Материал
        public AnimationCurve WidthCurve; // Рассеивание при движении
        public float Duration = 0.5f; // Продолжительность
        public float MinVertexDistance = 0.1f; // Минимальное расстояние до вершины
        public Gradient Color; // Цветовой градиент

        public float MissDistance = 100f; // Пропущенное расстояние - то, как далеко должны лететь пули после промаха
        public float SimulationSpeed = 100f; // Скорость моделирования - скорострельность
    }
}