using System;
using System.Collections.Generic;
using UnityEngine;
using ImpactSystem.Effects;

namespace ImpactSystem
{
    [CreateAssetMenu(menuName = "Impact System/Surface", fileName = "Surface")]
    public class Surface : ScriptableObject
    {
        [Serializable]
        public class SurfaceImpactTypeEffect
        {
            // Тип поверхностного удара
            public ImpactType ImpactType;
            // Воспроизводимый эффект
            public SurfaceEffect SurfaceEffect;
        }
        // Список эффектов типов воздействия на поверхность
        public List<SurfaceImpactTypeEffect> ImpactTypeEffects = new List<SurfaceImpactTypeEffect>();
    }
}