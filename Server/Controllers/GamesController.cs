using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TriangleDbRepository;
using TriangleProject.Shared.Models.Matchix;
using TriangleProject.Shared.Models.Portelem;
using TriangleProject.Shared.Models.Unity;

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
                    if (user != null)//אם קיים היוזר תראה את הרשימה הבאה של המשחקים
                    {
                        string gameQuery = "SELECT ID, GameCode, GameName, PairCount, IsPublish, canPublic FROM Games WHERE UserId = @UserId";
                        var gamesRecords = await _db.GetRecordsAsync<Game>(gameQuery, param);
                        user.Games = gamesRecords.ToList();
                        return Ok(user);
                    }
                    return BadRequest("משתמש לא קיים במערכת");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");
        }

        [HttpPost("addGame")]
        public async Task<IActionResult> AddGames(int userId, Game gameToAdd)
        {
            //בדיקה האם יש משתמש מחובר- בצד האורך
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId != null)
            {
                //בדיקה שהמשתמש שמנסה להוסיף משחק הוא אותו משתמש שמחובר
                if (userId == sessionId)
                {
                    object param = new
                    {
                        UserId = userId
                    };
                    string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
                    var userRecords = await _db.GetRecordsAsync<UserWithGames>(userQuery, param);
                    UserWithGames user = userRecords.FirstOrDefault();
                    //בדיקה שיש משתמש כזה במחולל שלנו
                    if (user != null)
                    {
                        object newGameParam = new
                        {
                            GameCode = 0,
                            GameName = gameToAdd.GameName,//לא יודעים זאת זה מגיע מבחוץ מהיוזר
                            canPublic = false,
                            PairCount = 18,
                            UserId = userId,//לא יודעים גם את זה, מגיע מבחוץ מהיוזר
                            IsPublish = false,//כרגע אין עדיין את המסיחים
                        };
                        string insertGameQuery = "INSERT INTO Games (GameCode, GameName, canPublic, PairCount, UserId, IsPublish)" +
                                "VALUES (@GameCode, @GameName, @canPublic, @PairCount, @UserId, @IsPublish)";
                        int newGameId = await _db.InsertReturnId(insertGameQuery, newGameParam);
                        if (newGameId != 0)
                        {
                            //אם המשחק נוצר בהצלחה, נחשב את הקוד עבורו
                            int gameCode = Convert.ToInt32(newGameId) + 100;//יצירת קוד משחק עם האיי די של המשתמש
                            object updateParam = new
                            {
                                ID = newGameId,
                                GameCode = gameCode
                            };
                            string updateCodeQuery = "UPDATE Games SET GameCode = @GameCode	WHERE ID=@ID";
                            bool isUpdate = await _db.SaveDataAsync(updateCodeQuery, updateParam);
                            if (isUpdate == true)
                            {
                                //אם המשחק עודכן בהצלחה- נחזיר את הפרטים שלו לעורך
                                object param2 = new
                                {
                                    ID = newGameId
                                };
                                string gameQuery = "SELECT ID, GameName, GameCode, PairCount, IsPublish, canPublic FROM Games WHERE ID = @ID";
                                var gameRecord = await _db.GetRecordsAsync<Game>(gameQuery, param2);
                                Game newGame = gameRecord.FirstOrDefault();
                                return Ok(newGame);
                            }
                            return BadRequest("Game code not created");
                        }
                        return BadRequest("Game not created");
                    }
                    return BadRequest("User Not Found");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");
        }

        //נבצע את שתי בדיקות ההתחברות הראשונות
        [HttpPost("publishGame")]
        public async Task<IActionResult> publishGame(int userId, PublishGame game)
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId != null)
            {
                if (userId == sessionId)
                {
                    // בבדיקה השלישית נרצה לוודא בנוסף לכך שהמשתמש קיים בבסיס הנתונים שהמשחק שהוא מבקש לעדכן שייך לו
                    object param = new
                    {
                        UserId = userId,
                        gameID = game.ID
                    };
                    string checkQuery = "SELECT GameName FROM Games WHERE UserId = @UserId and ID=@gameID";//נרצה לבדוק שהמשחק שנרצה לעדכן שייך ליוזר המחובר
                    var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
                    string gameName = checkRecords.FirstOrDefault();
                    if (gameName != null)
                    {
                        //במידה ונרצה לפרסם את המשחק, נבצע בדיקה רביעית שמטרתה לוודא שניתן לפרסם אותו, כלומר, שהשדה CanPublish הוא True
                        if (game.IsPublish == true)//הפיכת משחק לא מפורסם למפורסם
                        {
                            await CanPublishFunc(game.ID);//אם יש בעיה אם ה-id נתקן את זה בהמשך
                            object canPublishParam = new
                            {
                                gameID = game.ID
                            };
                            string PublicQuery = "SELECT canPublic FROM Games WHERE ID=@gameID";//שליפת מצב הפירסום לפי האפיון- אצלינו חייב 18 זוגות
                            var canPublicRecords = await _db.GetRecordsAsync<bool>(PublicQuery, canPublishParam);
                            bool IsPublish = canPublicRecords.FirstOrDefault();
                            if (IsPublish == false)
                            {
                                return BadRequest("המשחק אינו עומד בדרישות המינימום לפרסום");
                            }
                        }
                        //אם כל הבדיקות יצאו תקינות, כלומר:
                        //המשתמש מחובר
                        //המשחק שייך למשתמש שרוצה לעדכן אותו
                        //המשתמש רוצה לפרסם והמשחק ניתן לפרסום / המשתמש רוצה להסיר פרסום
                        //נוכל לעדכן את מצב פרסום המשחק!

                        string updateQuery = "UPDATE Games SET IsPublish=@IsPublish WHERE ID=@ID";
                        bool isUpdate = await _db.SaveDataAsync(updateQuery, game);
                        if (isUpdate == true)
                        {
                            return Ok();
                        }
                        return BadRequest("Update Failed");

                    }
                    return BadRequest("It's Not Your Game");
                }
                return BadRequest("User Not Logged In");
            }
            return BadRequest("No Session");
        }

        private async Task CanPublishFunc(int gameId)
        {
            int minQuestions = 18;//דרישת המינימום למשחק- 18 זוגות
            bool IsPublish = false;
            bool canPublic = false;

            object param = new
            {
                ID = gameId
            };
            //הרצת שאילתה ששולפת לפי המשחק שקיבלנו את כמות השאלות שיש. נקבל ערך מספרי
            string queryQuestionCount = "SELECT Count(ID) from Questions WHERE GameID=@ID";
            var recordQuestionCount = await _db.GetRecordsAsync<int>(queryQuestionCount, param);
            int numberOfQuestions = recordQuestionCount.FirstOrDefault();
            if (numberOfQuestions == minQuestions)// נבדוק אם המספר שקיבלנו גדול מדרישת המינימום שלנו
                
            {
                canPublic = true;// אם כן נשנה את הקוביה למצב true
            }

            if (canPublic == true)
            {
                string updateQuery = "UPDATE Games SET canPublic=true WHERE ID = @ID";
                bool isUpdate = await _db.SaveDataAsync(updateQuery, param);
                Console.WriteLine($"The update of game: {gameId} was completed successfully {isUpdate}");
            }
            else
            {
                string updateQuery = "UPDATE Games SET IsPublish=false, canPublic=false WHERE ID = @ID";
                bool isUpdate = await _db.SaveDataAsync(updateQuery, param);
                Console.WriteLine($"The update of game: {gameId} was completed successfully {isUpdate}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> GameToDelete(int id)
        {
            int? userId = HttpContext.Session.GetInt32("userId");
            object param = new
            {
                ID = id
            };
          
            string query = $"DELETE FROM Games WHERE Games.ID = {id} And Games.UserId = {userId}";
            bool isDelet = await _db.SaveDataAsync(query, param);

            if (isDelet == true)
            {
                return Ok();
            }
            return BadRequest("Delete failed");
        }

    }
}
