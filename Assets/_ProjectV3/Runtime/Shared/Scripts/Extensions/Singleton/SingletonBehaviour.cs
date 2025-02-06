using UnityEngine;

namespace _Project.Runtime.Core.Extensions.Singleton
{
  public class SingletonBehaviour<T> : MonoBehaviour, ISingleton where T : MonoBehaviour
  {
    public static T Instance
    {
      get
      {
        if (_instance != null)
        {
          return _instance;
        }

        if (FindObjectsByType<T>(FindObjectsSortMode.None) is T[] managers && managers.Length != 0)
        {
          if (managers.Length == 1)
          {
            _instance = managers[0];
            return _instance;
          }

          Debug.LogError("Class " + typeof(T).Name + " exists multiple times in violation of singleton pattern. Destroying all copies");
          foreach (T manager in managers)
          {
            Destroy(manager.gameObject);
          }
        }
        var go = new GameObject(typeof(T).Name, typeof(T));
        _instance = go.GetComponent<T>();

        SingletonManager.Add(_instance);

        return _instance;
      }
    }
    private static T _instance;

    public void Flush()
    {
      if (_instance == null)
        return;
      
      if (_instance.gameObject != null)
      {
        DestroyImmediate(_instance.gameObject);
      }

      _instance = null;
    }

  }
}
