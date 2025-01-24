namespace _Project.Runtime.Core.Extensions.Singleton
{
  public class Singleton<T> : ISingleton where T : class, new()
  {
    private static readonly object _lock = new object();
    private static T _instance;

    public static T Instance
    {
      get
      {
        if (_instance == null)
        {
          lock (_lock)
          {
            if (_instance == null)
            {
              _instance = new T();
              SingletonManager.Add(_instance);
            }
          }
        }
        return _instance;
      }
    }

    public void Flush()
    {
      lock (_lock)
      {
        _instance = null;
      }
    }
  }
}
