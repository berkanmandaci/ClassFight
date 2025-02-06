using System;
using UnityEngine;

namespace _Project.Runtime.Project.Game.Scripts.Vo
{
    public class NotificationsVo
    {
      
        public string Comment { get; set; }
        
        public long Time { get; set; }
        
        public Sprite NotificationSprite { get; set; }
        
        public NotificationType NotificationsType { get; set; }


        public Action Action { get; set; }
        
        
        

    }
    public enum NotificationType
    {
        FriendRequest,
        TrainingOver,
        LobbyInvite,
        ReferFriendUsedCode,
        FriendAccepted
    }
}