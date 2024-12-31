using System;
using _Project.Runtime.Core.Extensions.Singleton;
using UnityEngine;
namespace _Project.Runtime.Project.Service.Scripts.Model
{
    public class LogModel : Singleton<LogModel>
    {

        public void Error(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    Debug.LogError(innerException);
                }
            }
        }
        public void Log(string message, string ff4500)
        {
            Debug.Log("<color=#" + ff4500 + ">" + message + "</color>");
        }
        public void Log(string message)
        {
            Debug.Log(message);
        }

    }
}
