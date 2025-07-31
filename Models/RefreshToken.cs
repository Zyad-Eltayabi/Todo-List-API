namespace Todo_List_API.Models;

public class RefreshToken
{
    
    public int Id { get; set; }
    public string Token { get; set; } = null!;

    public DateTime ExpiresOn { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? RevokedOn { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;

    public bool IsActive => RevokedOn == null && !IsExpired;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}