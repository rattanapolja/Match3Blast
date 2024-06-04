using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [SerializeField] private List<GameObject> m_PrefabList;

    private Dictionary<string, Queue<GameObject>> m_PoolDictionary;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        m_PoolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (var prefab in m_PrefabList)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            m_PoolDictionary[prefab.name] = objectPool;
        }
    }

    public GameObject GetFromPool(string prefabName)
    {
        if (!m_PoolDictionary.ContainsKey(prefabName))
        {
            Debug.LogWarning("Pool with name " + prefabName + " doesn't exist.");
            return null;
        }

        if (m_PoolDictionary[prefabName].Count > 0)
        {
            GameObject obj = m_PoolDictionary[prefabName].Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            foreach (var prefab in m_PrefabList)
            {
                if (prefab.name == prefabName)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.name = prefabName;
                    return obj;
                }
            }
        }
        return null;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        m_PoolDictionary[obj.name].Enqueue(obj);
    }
}