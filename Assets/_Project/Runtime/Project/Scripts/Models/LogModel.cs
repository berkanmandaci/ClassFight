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
            throw new System.NotImplementedException();
        }
        public void Log(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}
