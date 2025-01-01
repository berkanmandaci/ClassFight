using _Project.Runtime.Core.Extensions.Singleton;
using _Project.Scripts.Vo;
using Nakama;

public class UserModel : SingletonBehaviour<UserModel>
{

    public UserVo UserVo { get; set; }
    public void Init()
    {
        UserVo = new UserVo(AuthenticationModel.Instance.Account.User);
    }


}
