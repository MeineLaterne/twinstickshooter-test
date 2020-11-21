using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool : MonoBehaviour {
    
    public int LastObtainedIndex { get; private set; }

    [SerializeField] private bool autoExpand;
    [SerializeField] private int poolSize;
    [SerializeField] private GameObject objectToPool;
    [SerializeField] private Transform parentTransform;

    private readonly List<GameObject> pool = new List<GameObject>();
    
    public GameObject Obtain(bool active = false) {
        for (var i = 0; i < pool.Count; i++) {
            var go = pool[i];
            if (!go.activeInHierarchy) {
                go.SetActive(active);
                LastObtainedIndex = i;
                return go;
            }
        }

        if (autoExpand) {
            var go = Instantiate(objectToPool);
            go.SetActive(active);
            pool.Add(go);
            LastObtainedIndex = pool.Count - 1;
            return go;
        }

        return null;
    }

    public void Free(GameObject pooledGameObject) => pooledGameObject.SetActive(false);

    private void Awake() {
        for (int i = 0; i < poolSize; i++) {
            var go = parentTransform == null ? 
                     Instantiate(objectToPool) : 
                     Instantiate(objectToPool, parentTransform);
            
            go.SetActive(false);
            pool.Add(go);
        }
    }


}