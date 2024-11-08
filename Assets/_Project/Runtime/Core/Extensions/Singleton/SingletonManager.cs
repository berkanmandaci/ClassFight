using System.Collections.Generic;
using UnityEngine;

namespace _Project.Runtime.Core.Extensions.Singleton
{
  public static class SingletonManager
  {
    private static List<ISingleton> _list = new List<ISingleton>();

    public static void Add<T>(T singleton) where T : class
    {
      Debug.Log("SingletonManager>Add " + typeof(T));
      _list.Add((ISingleton)singleton);
    }

    public static void Flush()
    {
      foreach (var singleton in _list)
      {
        singleton.Flush();
      }
      _list = new List<ISingleton>();
    }
  }
}
