namespace enum_Character
{
    /// <summary>
    /// @ CanPlayed : 유저가 플레이 가능
    /// @ CanNotPlayed : 유저가 플레이 불가능
    /// @ InterActing : 상호작용 중 ( 점프 불가 )
    /// </summary>
    public enum States { CanPlayed,CanNotPlayed, InterActing };
    public enum Kinds {None, Player, NPC };
    public enum Anims { IsRunning,IsJumping, PressJump, PressRunningJump, SwitchOn };
}