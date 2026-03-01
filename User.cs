public class User
{
    public int Id { get; set; }
  
    
    public string Username { get; set; } = null!;
public string Password { get; set; } = null!;// בעולם אמיתי נשמור Hash, כרגע נשמור טקסט פשוט
}