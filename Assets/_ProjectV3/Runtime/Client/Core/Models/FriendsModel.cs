using System.Collections.Generic;
using System.Linq;
using _Project.Runtime.Core.Extensions.Signal;
using Cysharp.Threading.Tasks;
using Nakama;
using ProjectV3.Shared.Extensions;
namespace ProjectV3.Client
{
    public class IncomingFriendRequestSignal : ASignal
    {
    }

    public class FriendsModel : Singleton<FriendsModel>
    {
        public List<IApiFriend> FriendList;
        public Dictionary<string, IApiFriend> MappedFriendList => FriendList.ToDictionary(vo => vo.User.Id, vo => vo);

        public void Load(List<IApiFriend> friendList)
        {
            FriendList = friendList;
        }

        public async UniTask ListFriends()
        {
            if (FriendList != null)
            {
                return;
            }

            // var friendList = new List<FriendsClubVo>();
            // try
            // {
            //     var serviceModel = ServiceModel.Instance;
            //     var result =
            //         await serviceModel.Client.ListFriendsAsync(AuthenticationModel.Instance.ActiveSession, null, 100);
            //     foreach (var f in result.Friends)
            //     {
            //         var friend = new FriendsClubVo();
            //         friend = friend.FromApiFriend(f);
            //         if (friend.State == 2)
            //         {
            //             var newNotification = new NotificationsVo
            //             {
            //                 Comment = string.Format("{0} sana arkadaşlık isteği gönderdi.", friend.Name),
            //                 // Comment = LocalizationSystem.GetLocalizedUi(LocalizeKey.FriendRequestComment),
            //                 Time = friend.UpdateTime,
            //                 NotificationsType = NotificationType.FriendRequest,
            //                 NotificationSprite = null,
            //                 // Action = () => FriendsController.AddFriend(friend).Forget()
            //             };
            //
            //             NotificationsModel.Instance.AddNotification(newNotification);
            //         }
            //
            //         friendList.Add(friend);
            //         LogModel.Instance.Log($"Friend '{f.User.Username}' state '{f.State}'");
            //     }
            //
            //     Load(friendList);
            //     FollowFriends();
            //     Signals.Get<SocketConnected>().AddListener(FollowFriends);
            //     Signals.Get<IncomingFriendRequestSignal>().Dispatch();
            // }
            // catch (Exception e)
            // {
            //     LogModel.Instance.Error(e.Message);
            //     throw;
            // }

            // Load(friendList);
        }

        public async void FollowFriends()
        {
            var friendsClubVos = FriendList.Where(x => x.State == 0).Select(x => x.User.Id).ToArray();
            await ServiceModel.Instance.FollowUsersAsync(friendsClubVos);
        }

        public void RemoveListener()
        {
            Signals.Get<SocketConnected>().RemoveListener(FollowFriends);
        }

        // public async UniTask LoadFriends()
        // {
        //     var friendList = new List<FriendsClubVo>();
        //     try
        //     {
        //         var serviceModel = ServiceModel.Instance;
        //         var result =
        //             await serviceModel.Client.ListFriendsAsync(AuthenticationModel.Instance.ActiveSession, null, 100);
        //         foreach (var f in result.Friends)
        //         {
        //             var friend = new FriendsClubVo();
        //             friend = friend.FromApiFriend(f);
        //             friendList.Add(friend);
        //             LogModel.Instance.Log($"Friend '{f.User.Username}' state '{f.State}'");
        //         }
        //
        //         Load(friendList);
        //     }
        //     catch (Exception e)
        //     {
        //         LogModel.Instance.Error(e.Message);
        //         throw;
        //     }
        // }

        public async UniTask ClearFriendRequest()
        {
            string[] ids = FriendList.Where(vo => vo.State == 2).Select(vo => vo.User.Id).ToArray();

            await ServiceModel.Instance.DeleteFriend(ids);
        }

        // public void UpdateFriend(IApiFriend friendsVo)
        // {
        //     foreach (var friend in FriendList)
        //     {
        //         if (friend.User.Id == friendsVo.User.Id)
        //         {
        //             friend.State = friendsVo.State;
        //         }
        //     }
        // }

        // public void AddFriendToList(IApiFriend friendsClubVo)
        // {
        //     var clubVo = FriendList.Find(x => x.Id == friendsClubVo.Id);
        //
        //     if (clubVo == null)
        //     {
        //         friendsClubVo.State = 1;
        //         FriendList.Add(friendsClubVo);
        //     }
        //     else
        //     {
        //         clubVo = friendsClubVo;
        //         clubVo.State = 1;
        //         FriendList.Add(clubVo);
        //     }
        // }
        //
        // public void AddFriend(FriendsClubVo friendsClubVo)
        // {
        //     var clubVo = FriendList.Find(x => x.Id == friendsClubVo.Id);
        //     
        //     clubVo.State = 0;
        //     if (!FriendList.Contains(clubVo))
        //     {
        //         FriendList.Add(clubVo);
        //     }
        //     else
        //     {
        //         FriendList.Find(vo => vo.Id == clubVo.Id).State = 0;
        //     }
        // }


        // public void RemoveFriendToList(string clubId)
        // {
        //     foreach (var club in FriendList)
        //     {
        //         if (club.Id == clubId)
        //         {
        //             FriendList.Remove(club);
        //             break;
        //         }
        //     }
        // }
    }
}