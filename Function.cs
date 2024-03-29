using Amazon.Lambda.Core;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.EntityFrameworkCore;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RoadieMovie;
public class Function
{

  DatabaseContext dbContext;
  public Function()
  {
    DotNetEnv.Env.Load();
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
    var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
    .UseNpgsql(connectionString)
    .Options;

    dbContext = new DatabaseContext(contextOptions);
  }

  async public Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
  {
    var method = request.RequestContext.Http.Method;
    var pathParameters = request.PathParameters;

    switch (method)
    {
      case "GET":
        if (pathParameters != null && pathParameters.ContainsKey("name"))
        {
          return await GetMoviesByName(pathParameters["name"]);
        }
        else if (pathParameters != null && pathParameters.ContainsKey("id"))
        {
          return await GetMovieById(int.Parse(pathParameters["id"]));
        }
        return GetMovies();
      case "POST":
        return await CreateMovie(request);
      case "PUT":
        return await UpdateMovie(request);
      case "DELETE":
        return await DeleteMovie(request);
      default:
        return new APIGatewayHttpApiV2ProxyResponse
        {
          StatusCode = (int)HttpStatusCode.MethodNotAllowed,
          Body = "Method not allowed"
        };
    }
  }

  public class MovieWithRatings
  {
    public Movie Movie { get; set; }
    public List<int> Ratings { get; set; }
  }


  private APIGatewayHttpApiV2ProxyResponse GetMovies()
  {
    var moviesWithRatings = dbContext.Movies
        .Select(m => new MovieWithRatings
        {
          Movie = m,
          Ratings = m.UserMovieRatings.Select(umr => umr.Rating).ToList()
        })
        .ToList();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.OK,
      Body = System.Text.Json.JsonSerializer.Serialize(moviesWithRatings),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }


  private async Task<APIGatewayHttpApiV2ProxyResponse> GetMoviesByName(string name)
  {
    var moviesWithRatings = await dbContext.Movies
        .Where(m => m.Name.Contains(name))
        .Select(m => new MovieWithRatings
        {
          Movie = m,
          Ratings = m.UserMovieRatings.Select(umr => umr.Rating).ToList()
        })
        .ToListAsync();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.OK,
      Body = System.Text.Json.JsonSerializer.Serialize(moviesWithRatings),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }


  private async Task<APIGatewayHttpApiV2ProxyResponse> GetMovieById(int id)
  {
    var movieWithRatings = await dbContext.Movies
        .Where(m => m.Id == id)
        .Select(m => new MovieWithRatings
        {
          Movie = m,
          Ratings = m.UserMovieRatings.Select(umr => umr.Rating).ToList()
        })
        .FirstOrDefaultAsync();

    if (movieWithRatings == null)
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.NotFound,
        Body = "Movie not found",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.OK,
      Body = System.Text.Json.JsonSerializer.Serialize(movieWithRatings),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }


  private async Task<APIGatewayHttpApiV2ProxyResponse> CreateMovie(APIGatewayHttpApiV2ProxyRequest request)
  {
    var movie = System.Text.Json.JsonSerializer.Deserialize<Movie>(request.Body);

    dbContext.Movies.Add(movie);
    await dbContext.SaveChangesAsync();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.Created,
      Body = System.Text.Json.JsonSerializer.Serialize(movie),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> UpdateMovie(APIGatewayHttpApiV2ProxyRequest request)
  {
    var pathParameters = request.PathParameters;

    if (pathParameters != null && pathParameters.ContainsKey("id"))
    {
      var id = int.Parse(pathParameters["id"]);
      var movieToUpdate = await dbContext.Movies.FindAsync(id);

      if (movieToUpdate == null)
      {
        return new APIGatewayHttpApiV2ProxyResponse
        {
          StatusCode = (int)HttpStatusCode.NotFound,
          Body = "Movie not found",
          Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
      }

      var movie = System.Text.Json.JsonSerializer.Deserialize<Movie>(request.Body);
      movie.Id = id; // Set the movie id from path parameters

      dbContext.Movies.Update(movie);
      await dbContext.SaveChangesAsync();

      var response = new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.OK,
        Body = System.Text.Json.JsonSerializer.Serialize(movie),
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
      return response;
    }
    else
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Body = "Invalid request",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> DeleteMovie(APIGatewayHttpApiV2ProxyRequest request)
  {
    var movieId = int.Parse(request.PathParameters["id"]);

    var movie = await dbContext.Movies.FindAsync(movieId);
    if (movie == null)
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.NotFound,
        Body = "Movie not found",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }
    dbContext.Movies.Remove(movie);
    await dbContext.SaveChangesAsync();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.OK,
      Body = System.Text.Json.JsonSerializer.Serialize(new { message = "Movie deleted successfully" }),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }
}