﻿using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool : MonoBehaviour {
    
    public GameObjectPool Instance { get; private set; }

    [SerializeField] private bool dontDestroyOnLoad;
    [SerializeField] private bool autoExpand;
    [SerializeField] private bool activateOnObtain;
    [SerializeField] private int poolSize;
    [SerializeField] private GameObject objectToPool;

    private readonly List<GameObject> pool = new List<GameObject>();
    
    public GameObject Obtain() {
        foreach (var go in pool) {
            if (!go.activeInHierarchy) {
                if (activateOnObtain) go.SetActive(true);
                return go;
            }
                
        }

        if (autoExpand) {
            var go = Instantiate(objectToPool);
            if(!activateOnObtain) go.SetActive(false);
            pool.Add(go);
            return go;
        }

        return null;
    }

    public void Free(GameObject pooledGameObject) => pooledGameObject.SetActive(false);

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        for (int i = 0; i < poolSize; i++) {
            var go = Instantiate(objectToPool);
            go.SetActive(false);
            pool.Add(go);
        }
    }


}