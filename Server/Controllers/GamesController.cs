using System.Linq;
using System.Reflection.Metadata.Ecma335;
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

        [HttpGet] // אין צורך להוסיף פרמטר הוא נמצא בנתיב השיטה  
        public async Task<IActionResult> GetGamesByUser(int userId)
        {
            // בדיקה האם קיים סשן פעיל
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (userId != sessionId)
            {
                return BadRequest("No Session");
            }
            
            //בדיקה האם קיים משתמש מחובר
            object param = new
            {
                UserId = userId
            };

            string userQuery = "SELECT FirstName FROM Users WHERE ID = @UserId";
            bool userExists  = (await _db.GetRecordsAsync<PortelemUser>(userQuery, param)).Any();
            
            if (!userExists)
            {
                return BadRequest("משתמש לא קיים במערכת");
            }

            //אם קיים היוזר תראה את הרשימה הבאה של המשחקים
            string gameListQuery = "SELECT ID, GameCode, GameName, PairCount, IsPublish, canPublic FROM Games WHERE UserId = @UserId";
            List<Game> GameList = (await _db.GetRecordsAsync<Game>(gameListQuery, param)).ToList();
            return Ok(GameList);
        }

        [HttpPost("addGame")]
        public async Task<IActionResult> AddGame(int userId, Game gameToAdd)
        {
            //בדיקה האם יש משתמש מחובר- בצד העורך
            int? sessionId = HttpContext.Session.GetInt32("userId");
            
            //בדיקה שהמשתמש שמנסה להוסיף משחק הוא אותו משתמש שמחובר
            if(sessionId == null)
            {
                return BadRequest("No Session");
            }

            //בדיקה שיש משתמש כזה במחולל שלנו
            string userQuery = "SELECT * FROM Users WHERE ID = @UserId";
            object userExistsParameters = new
            {
                UserId = userId
            };

            bool userExists = (await _db.GetRecordsAsync<PortelemUser>(userQuery, userExistsParameters)).Any();

            if(!userExists)
            {
                return BadRequest("User Not Logged In");
            }

            int gameCode = await GetUniqueGameCode();
            object insertGameParameters = new
            {
                GameCode = gameCode,
                GameName = gameToAdd.GameName,//לא יודעים זאת זה מגיע מבחוץ מהיוזר
                canPublic = false,
                PairCount = 18,
                UserId = userId,//לא יודעים גם את זה, מגיע מבחוץ מהיוזר
                IsPublish = false,//כרגע אין עדיין את המסיחים
            };
            string insertGameQuery = "INSERT INTO Games (GameCode, GameName, canPublic, PairCount, UserId, IsPublish)" +
                    "VALUES (@GameCode, @GameName, @canPublic, @PairCount, @UserId, @IsPublish)";
            await _db.SaveDataAsync(insertGameQuery, insertGameParameters);
            return Ok(insertGameParameters);
        }

        //נבצע את שתי בדיקות ההתחברות הראשונות
        [HttpPost("publishGame")]
        public async Task<IActionResult> publishGame(int userId, Game game)
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId == null)
            {
                return BadRequest("No Session");
            }

            {
                if (userId != sessionId)
                {
                    return BadRequest("User not logged in");
                }


                // בבדיקה השלישית נרצה לוודא בנוסף לכך שהמשתמש קיים בבסיס הנתונים שהמשחק שהוא מבקש לעדכן שייך לו
                object param = new
                {
                    UserId = userId,
                    game.GameCode
                };
                string checkQuery = "SELECT GameName FROM Games WHERE UserId = @UserId and GameCode=@GameCode";//נרצה לבדוק שהמשחק שנרצה לעדכן שייך ליוזר המחובר
                var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
                string gameName = checkRecords.FirstOrDefault();
                if (gameName == null)
                {
                    return BadRequest("It's not your game");
                }

                //במידה ונרצה לפרסם את המשחק, נבצע בדיקה רביעית שמטרתה לוודא שניתן לפרסם אותו, כלומר, שהשדה CanPublish הוא True
                if (game.IsPublish)//הפיכת משחק לא מפורסם למפורסם
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

        [HttpDelete("{gameCode}")]
        public async Task<IActionResult> GameToDelete(int gameCode)
        {
            int? userId = HttpContext.Session.GetInt32("userId");
            if(userId == null)
            {
                return BadRequest("No Session");
            }

            object param = new
            {
                GameCode= gameCode
            };
          
            string query = $"DELETE FROM Games WHERE Games.GameCode={gameCode} AND Games.UserId={userId}";
            bool isDeleted = await _db.SaveDataAsync(query, param);

            if (isDeleted == true)
            {
                return Ok("Deleted Game Successfully");
            }

            return BadRequest("Delete failed");
        }

        private async Task<int> GetUniqueGameCode()
        {
            Random rand = new Random();
            int minValue = 10000, maxValue = 100000;
            int gameCode;
            bool gameCodeAlreadyInUse = true;
            
            do
            {
                gameCode = rand.Next(minValue, maxValue);
                
                string query = "SELECT * FROM Games WHERE GameCode=@gameCode";
                object queryParameters = new
                {
                    gameCode
                };

                gameCodeAlreadyInUse = (await _db.GetRecordsAsync<Game>(query, queryParameters)).Any();
            } 
            while(gameCodeAlreadyInUse);

            return gameCode;
        }
    }
}
