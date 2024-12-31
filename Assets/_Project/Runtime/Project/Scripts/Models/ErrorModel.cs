using System;
using System.Collections.Generic;
using _Project.Runtime.Core.Extensions.Singleton;
using UnityEngine;
namespace _Project.Runtime.Project.Service.Scripts.Model
{
    public class ErrorModel : Singleton<ErrorModel>
    {

        private readonly Dictionary<long, Action> _errorActions = new();
    
        public void AddErrorAction(long code, Action action)
        {
            if (_errorActions.ContainsKey(code))
            {
                Debug.Log("Error Action already added! "  + code);
                return;
            }

            _errorActions[code] = action;
        }
    
        public void RemoveActionByCode(long code)
        {
            if (_errorActions.ContainsKey(code))
            {
                _errorActions.Remove(code);
            }
        }
    
        public void RunByCode(long statusCode)
        {
            if (_errorActions.ContainsKey(statusCode))
            {
                _errorActions[statusCode].Invoke();
            }

        }
    }
}
