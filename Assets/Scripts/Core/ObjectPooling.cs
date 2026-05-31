using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Drawing;

[System.Serializable]
public class Pool
{
    [SerializeField] string tagName; // Tên của pool, có thể dùng để phân biệt các pool khác nhau
    [SerializeField] public GameObject pooledObjects; // Danh sách chứa các đối tượng trong pool
    [SerializeField] public int size;
    public string TagName { get { return tagName; } }
}
public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling Instance; // Biến singleton để truy cập từ các script khác
    public List<Pool> pools; // Danh sách các pool
    public Dictionary<string, Queue<GameObject>> poolDictionary; // Từ điển để quản lý các pool theo tên
    private Dictionary<string, GameObject> prefabDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Thì hủy đối tượng này đi để đảm bảo chỉ có một Instance duy nhất
            Debug.LogWarning("Phát hiện một ObjectPooling bị trùng lặp. Tự hủy đối tượng thừa.");
            Destroy(gameObject);
            return; // Rất quan trọng, thoát khỏi hàm Awake ngay lập tức
        }

        // Nếu không, hãy gán Instance là chính nó
        Instance = this;
    }

    //Bể chứa game objects
    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>();
        foreach (Pool pool in pools)
        {
            //định nghĩa Queue trong Dictionary
            Queue<GameObject> gameObjects = new Queue<GameObject>();
            prefabDictionary.Add(pool.TagName, pool.pooledObjects);
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.pooledObjects);
                obj.SetActive(false); // Đặt trạng thái ban đầu là không hoạt động
                gameObjects.Enqueue(obj); // Thêm vào hàng đợi
            }
                poolDictionary.Add(pool.TagName, gameObjects); // Thêm vào từ điển với tag là khóa
        }
    }

    public GameObject SpawnFromBool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning(tag + " không tồn tại trong Dictionary");
            return null;
        }
        if(poolDictionary[tag].Count == 0)
        {
            if (prefabDictionary.ContainsKey(tag))
            {
                GameObject newObj = Instantiate(prefabDictionary[tag]);
                newObj.SetActive(false); // Đặt trạng thái ban đầu là không hoạt động
                newObj.transform.position = position;
                newObj.transform.rotation = rotation;
                newObj.SetActive(true);
                return newObj;
            }
            else
            {
                return null;
            }
        }
        GameObject objectToSpawn = poolDictionary[tag].Dequeue(); // Lấy đối tượng từ hàng đợi
        objectToSpawn.SetActive(true); // Kích hoạt đối tượng
        objectToSpawn.transform.position = position; // Đặt vị trí
        objectToSpawn.transform.rotation = rotation; // Đặt xoay
        return objectToSpawn; // Trả về đối tượng đã kích hoạt
    }
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning(tag + " không tồn tại trong Dictionary");
            Destroy(objectToReturn); // Nếu không có pool, hủy đối tượng
            return;
        }
        objectToReturn.SetActive(false); // Đặt trạng thái là không hoạt động
        poolDictionary[tag].Enqueue(objectToReturn); // Thêm lại vào hàng đợi
    }
}
