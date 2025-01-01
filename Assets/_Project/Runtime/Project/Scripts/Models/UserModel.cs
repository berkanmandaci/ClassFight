using _Project.Runtime.Core.Extensions.Singleton;
using _Project.Scripts.Vo;
using Nakama;
using System.Collections.Generic;
using UnityEngine;

public class UserModel : SingletonBehaviour<UserModel>
{
    private Dictionary<string, UserVo> _connectedUsers = new Dictionary<string, UserVo>();
    public UserVo UserVo { get; private set; }

    public void Init()
    {
        UserVo = new UserVo(AuthenticationModel.Instance.Account.User);
        _connectedUsers[UserVo.Id] = UserVo;
    }

    public UserVo GetOrCreateUserVo(string userId, IApiUser apiUser = null)
    {
        if (_connectedUsers.TryGetValue(userId, out var existingUser))
        {
            return existingUser;
        }

        if (apiUser == null)
        {
            Debug.LogWarning($"User data not found for ID: {userId}");
            return null;
        }

        var newUser = new UserVo(apiUser);
        _connectedUsers[userId] = newUser;
        return newUser;
    }

    public void UpdateUserData(string userId, IApiUser apiUser)
    {
        var userVo = GetOrCreateUserVo(userId, apiUser);
        if (userVo != null)
        {
            // Burada gerekirse ek güncelleme işlemleri yapılabilir
            Debug.Log($"User data updated for {userVo.DisplayName}");
        }
    }

    public bool IsUserConnected(string userId)
    {
        return _connectedUsers.ContainsKey(userId);
    }

    public void RemoveUser(string userId)
    {
        if (_connectedUsers.ContainsKey(userId))
        {
            _connectedUsers.Remove(userId);
            Debug.Log($"User removed: {userId}");
        }
    }
}
