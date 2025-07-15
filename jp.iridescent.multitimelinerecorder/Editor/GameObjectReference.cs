using System;
using UnityEngine;
using UnityEditor;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// シーン内のGameObjectへの参照を保存可能な形式で管理するクラス
    /// </summary>
    [Serializable]
    public class GameObjectReference : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string scenePath = "";
        
        [SerializeField] 
        private string objectName = "";
        
        [SerializeField]
        private string tag = "";
        
        // Direct GameObject reference for Editor
        [SerializeField]
        private GameObject directReference;
        
        [NonSerialized]
        private GameObject cachedGameObject;
        
        public GameObject GameObject
        {
            get
            {
                // First try direct reference (most reliable in Editor)
                if (directReference != null)
                {
                    return directReference;
                }
                
                // Then try cached reference
                if (cachedGameObject != null)
                {
                    return cachedGameObject;
                }
                
                // Finally try to find by path
                if (!string.IsNullOrEmpty(scenePath))
                {
                    cachedGameObject = GameObject.Find(scenePath);
                    
                    // パスで見つからない場合、名前とタグで検索
                    if (cachedGameObject == null && !string.IsNullOrEmpty(objectName))
                    {
                        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                        foreach (var obj in allObjects)
                        {
                            if (obj.name == objectName)
                            {
                                if (string.IsNullOrEmpty(tag) || obj.CompareTag(tag))
                                {
                                    cachedGameObject = obj;
                                    break;
                                }
                            }
                        }
                    }
                }
                
                return cachedGameObject;
            }
            set
            {
                directReference = value;
                cachedGameObject = value;
                if (value != null)
                {
                    scenePath = GetGameObjectPath(value);
                    objectName = value.name;
                    tag = value.tag;
                }
                else
                {
                    scenePath = "";
                    objectName = "";
                    tag = "";
                }
            }
        }
        
        /// <summary>
        /// GameObjectのシーン内パスを取得
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        /// <summary>
        /// Transform参照を作成
        /// </summary>
        public static GameObjectReference FromTransform(Transform transform)
        {
            var reference = new GameObjectReference();
            reference.GameObject = transform != null ? transform.gameObject : null;
            return reference;
        }
        
        /// <summary>
        /// Transformを取得
        /// </summary>
        public Transform GetTransform()
        {
            var go = GameObject;
            return go != null ? go.transform : null;
        }
        
        // ISerializationCallbackReceiver implementation
        public void OnBeforeSerialize()
        {
            // Ensure path information is up to date before serialization
            if (directReference != null)
            {
                scenePath = GetGameObjectPath(directReference);
                objectName = directReference.name;
                tag = directReference.tag;
            }
        }
        
        public void OnAfterDeserialize()
        {
            // Cache will be rebuilt on first access
            cachedGameObject = null;
        }
    }
}