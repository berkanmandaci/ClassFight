using _Project.Runtime.Core.Extensions.Singleton;

public class UserModel : SingletonBehaviour<UserModel>
{
    public long Id { get; set; }

    public string NickName { get; set; }

    public int Level { get; set; }

    public float Experience { get; set; }

    public float ExperienceToNextLevel { get; set; }

    public int Elo { get; set; }


    public void Init()
    {
        
    }

}
