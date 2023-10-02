using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TriangleDbRepository;
using TriangleProject.Shared.Models.Matchix;
using TriangleProject.Shared.Models.Unity;

namespace TriangleProject.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnityController : ControllerBase
    {
        private readonly DbRepository _db;

        public UnityController(DbRepository db)
        {
            _db = db;
        }
        [HttpGet("getgame")]
        public async Task<IActionResult> GetGameByCode(int GameCode, int UserId)
        {
            object param = new
            {
                gameCode = GameCode,
                userId = UserId
            };
            string userQuery = "SELECT ID FROM Games WHERE GameCode=@gameCode AND UserId=@userId";//שאילתה לשליפת התאמת קוד נתון מהיוזר לקוד קיים במערכת
            var userRecords = await _db.GetRecordsAsync<int>(userQuery, param);
            int ID = userRecords.FirstOrDefault();//קליטת הקוד והצגתו כקוד היחיד הקיים

            if (ID > 0)//אם קיים קוד משחק במערכת
            {
                object param2 = new
                {
                    Id = ID
                };

                string gameQuery = "SELECT IsPublish FROM Games WHERE IsPublish =True AND ID = @Id";//שאילתה לבדיקה אם המשחק מפורסם
                var gamesRecords = await _db.GetRecordsAsync<bool>(gameQuery, param2);//
                bool isShow = gamesRecords.FirstOrDefault();

                if (ID > 0 && isShow == true)
                {

                    string infoQuery = "SELECT GameName FROM Games WHERE ID = @Id";
                    var gameRecords = await _db.GetRecordsAsync<Game>(infoQuery, param2);
                    Game game = gameRecords.FirstOrDefault();
                    if(game != null)
                    {
                        object param3 = new
                        {
                        GameId = ID
                        };
                    
                        string infoGame = "SELECT ID BoardItemContect, StockItemContent FROM Pairs WHERE GameId = @GameId";
                        var gamePair = await _db.GetRecordsAsync<GamePair>(infoGame, param3);
                        List<GamePair> PairList= gamePair.ToList();

                        object response = new
                        {
                            game.GameName,
                            game.GameCode,
                            game.UserId,
                            PairList
                        };

                        return Ok(response);// החזר את כל המידע הפנימי של המשחק
                    }
                   
                }

                return BadRequest("Game is not public");// הודעת אי פרסום של המשחק
            }

            return BadRequest("Game Not Found");//הודעת אי מציאת קוד משחק בבסיס הנתונים
        }
    }
}


