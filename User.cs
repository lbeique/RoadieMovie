using System.ComponentModel.DataAnnotations;

namespace RoadieMovie;

public class User
{
  [Key]
  public string Sub { get; set; }
  public string Username { get; set; }

}