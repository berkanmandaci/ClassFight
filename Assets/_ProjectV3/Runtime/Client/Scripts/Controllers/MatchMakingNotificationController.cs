using System;
using _Project.Runtime.Project.Game.Scripts.Vo;
using Nakama;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core.Controllers;
namespace ProjectV3.Client
{
    public class MatchMakingNotificationController : BaseNotificationController<MatchMakingNotificationController>
    {

        public override async void Run(IApiNotification obj)
        {
            try
            {
                var matchMakingResponseVo = Deserialize<MatchMakingResponseVo>(obj);

                await MatchmakingModel.Instance.JoinMatch(matchMakingResponseVo.MatchId);
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw; // TODO handle exception
            }

            try
            {

                await PvpServerModel.Instance.OnMatchFound();

            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw; // TODO handle exception
            }
        }
    }
}
