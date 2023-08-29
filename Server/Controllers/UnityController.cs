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
        [HttpGet("{GameCode}")]
        public async Task<IActionResult> GetGamesByCode(int GameCode)
        {
            object param = new
            {
                gameCode = GameCode
            };
            string userQuery = "SELECT ID FROM Games WHERE GameCode=@gameCode";//שאילתה לשליפת התאמת קוד נתון מהיוזר לקוד קיים במערכת
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

                    string infoQuery = "SELECT GameName, GameTime FROM Games WHERE ID = @Id";
                    var gameRecords = await _db.GetRecordsAsync<GameForUnity>(infoQuery, param2);
                    GameForUnity gameDeatils = gameRecords.FirstOrDefault();
                    if(gameDeatils != null)
                    {
                     object param3 = new
                     {
                        GameId = ID
                     };
                        string infoGame = "SELECT ID BoardItemContect, StockItemContent FROM Pairs WHERE GameId = @GameId";
                        var gamePair = await _db.GetRecordsAsync<Pairs>(infoGame, param3);
                        gameDeatils.PairList= gamePair.ToList();

                        return Ok(gameDeatils);// החזר את כל המידע הפנימי של המשחק
                    }
                   
                }

                return BadRequest("Game is not public");// הודעת אי פרסום של המשחק
            }

            return BadRequest("Game Not Found");//הודעת אי מציאת קוד משחק בבסיס הנתונים
        }

    }
}


