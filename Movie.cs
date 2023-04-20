namespace RoadieMovie;

public class Movie
{

  public int Id { get; set; }
  public string? Name { get; set; }
  public string? Director { get; set; }
  public string? Genre { get; set; }
  public string? ReleaseDate { get; set; }
  public string? PosterURL { get; set; }
  public virtual ICollection<UserMovieRating> UserMovieRatings { get; set; } = new List<UserMovieRating>();

}