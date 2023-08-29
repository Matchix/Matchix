using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TriangleDbRepository;
using TriangleProject.Shared.Models.Portelem;


namespace TriangleProject.Server.Controllers
{
    [Route("api/[controller]/{userId}")]//הוספת הפרמטר לנתיב בשיטה- גלובלי
    [ApiController]
    public class GamesController : ControllerBase
    {
        private DbRepository _db;

        public GamesController(DbRepository db)
        {
            _db = db;
        }

        //[HttpGet("{userId}")]// העברת הפרמטר בתוך נתיב ספציפי
        [HttpGet] // אין צורך להוסיף פרמטר הוא נמצא בנתיב השיטה  
        public async Task<IActionResult> GetGamesByUser(int userId)
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId != null)// האם יש משתמש מחובר?
            {
                if (userId == sessionId)
                {
                    object param = new
                    {
                        UserId = userId
                    };
                    string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
                    var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
                    UserWithGames user = userRecords.FirstOrDefault();
                    if (user != null)
                    {
                        string gameQuery = "SELECT GameName FROM Games WHERE UserId = @UserId";
                        var gamesRecords = await _db.GetRecordsAsync<string>(gameQuery, param);
                        user.Games = gamesRecords.ToList();
                        return Ok(user);
                    }
                    return BadRequest("משתמש לא קיים במערכת");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");
        }
       }  
    }
