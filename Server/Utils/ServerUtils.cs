
using TriangleDbRepository;
using TriangleProject.Shared.Models.Portelem;
using TriangleProject.Shared.Models.Unity;

public static class ServerUtils
{
    public static async Task<PortelemUser> GetUser(int userId, DbRepository db)
    {
        object userQueryParameters = new
        {
            ID = userId
        };

        string userQuery = $"Select * FROM Users WHERE ID=@ID";
        PortelemUser currentUser = (await db.GetRecordsAsync<PortelemUser>(userQuery, userQueryParameters)).FirstOrDefault();
        return currentUser;
    }

    public static async Task<PortelemUser> GetLoggedInUser(DbRepository db, HttpContext context)
    {
        int? sessionId = context.Session.GetInt32("userId");
        if (sessionId == null)
        {
            return null;
        }

        return await GetUser((int)sessionId, db);
    }

    public static async Task<List<Game>> GetLoggedInUserGameList(DbRepository db, HttpContext context)
    {
        PortelemUser user = await GetLoggedInUser(db, context);

        if (user == null)
        {
            return null;
        }

        object queryParameters = new
        {
            UserId = user.ID
        };

        string gameListQuery = "SELECT ID, GameCode, GameName, PairCount, IsPublish, canPublic FROM Games WHERE UserId = @UserId";
        List<Game> GameList = (await db.GetRecordsAsync<Game>(gameListQuery, queryParameters)).ToList();
        return GameList;
    }
}