namespace EcommerceLaptop.Core.DTOs;

public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int InactiveUsers { get; set; }
}
