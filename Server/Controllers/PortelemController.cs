using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TriangleDbRepository;
using TriangleProject.Shared.Models.Matchix;
using TriangleProject.Shared.Models.Portelem;

namespace TriangleProject.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortelemController : ControllerBase
    {
        private readonly DbRepository _db;
        private readonly IConfiguration _config;

        public PortelemController(DbRepository db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        [HttpGet("token")]
        public async Task<IActionResult> GetToken()
        {
            var userCookie = Request.Cookies["token"];
            if (userCookie != null)
            {
                return Ok(userCookie);
            }
            return BadRequest("no cookie found");
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetSystemId()
        {
            

            var configData = _config.GetSection("PortelemData");
            DataForSystem data = new DataForSystem()
            {
                SystemId = configData.GetValue("SystemId", 0),
                Url = configData.GetValue("Url", string.Empty)
            };

            if (data.SystemId == 0 || string.IsNullOrEmpty(data.Url))
            {
                return BadRequest("no data found");
            }
            return Ok(data);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(PortelemUser user)
        {
            object getParam = new
            {
                portelemId = user.PortelemId
            };
            string getQuery = "SELECT ID FROM Users WHERE PortelemId = @portelemId";
            var getRecords = await _db.GetRecordsAsync<int>(getQuery, getParam);
            int userId = getRecords.FirstOrDefault();
            if (userId == 0)
            {
                string insertQuery = "INSERT INTO Users (FirstName, LastName, PortelemId) VALUES (@FirstName, @LastName, @PortelemId)";
                userId = await _db.InsertReturnId(insertQuery, user);
                if (userId == 0)
                {
                    return BadRequest("user creation error");
                }
            }

            HttpContext.Session.SetInt32("userId", userId);
            return Ok(userId);

        }

        [HttpGet("activeuser")]
        public async Task<IActionResult> GetLoggedInUser()
        {
            int? sessionId = HttpContext.Session.GetInt32("userId");
            if (sessionId == null)
            {
                return BadRequest("No user signed in");
            }

            object userQueryParameters = new
            {
                ID = sessionId 
            };

            string userQuery = $"Select * FROM Users WHERE ID=@ID";
            PortelemUser currentUser = (await _db.GetRecordsAsync<PortelemUser>(userQuery, userQueryParameters)).FirstOrDefault();
            Console.WriteLine(JsonSerializer.Serialize(currentUser));
            return Ok(currentUser);
        }
    }
}
