using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InventorySaveManager : MonoBehaviour
{
    public static InventorySaveManager instance;

    public Transform GUIParent;
    public static SaveData _saveData = new SaveData();
    public GameObject[] itemPrefabs;

    private List<InventoryItem> currentItems;


    [System.Serializable]
    public struct SaveData
    {
        public ItemSaveState[] itemsData;
    }

    private void Awake()
    {
        if (instance != null)
            Destroy(this);

        instance = this;
        DontDestroyOnLoad(this);

        CreateFoldersAndFile();

        TryLoad();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            Save();
        }
    }

    public void SubscribeItem(InventoryItem item)
    {
        if (currentItems == null) currentItems = new();
        currentItems.Add(item);
    }

    public static string SaveFileName()
    {
        string saveFile = Application.persistentDataPath + "/data/" + "items.save";
        return saveFile;
    }

    public void CreateFoldersAndFile()
    {
        
        if(!Directory.Exists(Application.persistentDataPath + "/data")){
            Directory.CreateDirectory(Application.persistentDataPath + "/data/");
        }
        if(!File.Exists(Application.persistentDataPath + "/data/items.save")){
            File.Create(Application.persistentDataPath + "/data/items.save");
        }
        
    }

    public void Save()
    {
        SaveItems();
        CreateFoldersAndFile();
        File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));
    }

    private void SaveItems()
    {
        List<InventoryItem> itemsInsideInventory = new();
        foreach(var item in currentItems)
        {
            if (item.insideInventory == true)
                itemsInsideInventory.Add(item);
        }

        _saveData.itemsData = new ItemSaveState[itemsInsideInventory.Count];

        for(int i = 0; i < itemsInsideInventory.Count; i++)
        {
             itemsInsideInventory[i].SaveState(ref _saveData.itemsData[i]);
        }
    }

    public void TryLoad()
    {
        if (File.Exists(SaveFileName()))
            Load();
    }

    public void Load()
    {
        string saveContent = File.ReadAllText(SaveFileName());
        try
        {
            _saveData = JsonUtility.FromJson<SaveData>(saveContent);
            print(_saveData);
            HandleLoad();
        }
        catch(Exception e)
        {
            print("Nothing on file, good game!");
        }
        
    }

    public void HandleLoad()
    {
        foreach(var item in _saveData.itemsData)
        {
            print("Type = " + item.typeOfItem);
            var obj = Instantiate(itemPrefabs[(int)item.typeOfItem], GUIParent).GetComponent<InventoryItem>();
            obj.LoadState(item);
        }
    }

    public void Reload()
    {
        Save();

        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        bool guiParentIsInvalid = GUIParent == null || GUIParent.gameObject.scene.name != scene.name;
        if (guiParentIsInvalid)
        {
            GameObject found = GameObject.Find("GUI");
            if (found != null)
            {
                GUIParent = found.transform;
                Debug.Log("InventorySaveManager: GUIParent reassigned via GameObject.Find(\"GUIParent\").");
            }
            else
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    GUIParent = canvas.transform;
                    Debug.Log("InventorySaveManager: GUIParent reassigned to first Canvas found in scene.");
                }
                else
                {
                    Debug.LogWarning("InventorySaveManager: GUIParent not found in scene. Items will be instantiated at root.");
                    GUIParent = null; 
                }
            }
        }

        if (currentItems != null)
            currentItems.Clear();

        TryLoad();
    }
}
