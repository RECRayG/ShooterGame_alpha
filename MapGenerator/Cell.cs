using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public bool isCollapsed;
    public List<Prototype> possiblePrototypes;
    public List<int> prototypeWeights;
    public Vector2 coords = new Vector2();
    public Cell posXneighbour;
    public Cell negXneighbour;
    public Cell posZneighbour;
    public Cell negZneighbour;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    
    public Prototype currentSinglePrototype;
    public Transform playerSpawn;
    public bool visited;
    
    public void GenerateWeight(Weights weights)
    {
        prototypeWeights = new List<int>(new int[possiblePrototypes.Count]);
        int i = 0;
        foreach(Prototype p in possiblePrototypes)
        {
            if(p.attributes.Count==0)
            {
                // если атрибуты не были установлены в инспекторе, то по умолччанию вес = 5
                prototypeWeights[i] = 5;
            }
            else
            {
                // для остальных прототиппов устанавливаем вес, согласно настройкам атрибута
                foreach (Attribute attribute in p.attributes)
                    prototypeWeights[i] += weights.GetWeight(attribute);

                prototypeWeights[i] = (int)((float)prototypeWeights[i]/ (float)p.attributes.Count);
            }
            i++;
        }
    }
}