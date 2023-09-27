using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TriangleFileStorage;

namespace TriangleProject.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly FilesManage _filesManage;

        public MediaController(FilesManage filesManage)
        {
            _filesManage = filesManage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64)
        {
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadedFiles");
            return Ok(fileName);
        }

        [HttpPost("deleteImages")]
        public async Task<IActionResult>DeleteImages([FromBody] List<string> images)
        {
            var countFalseTry = 0;//בדיקת כמות השגיאות שהיו לנו
            foreach (string img in images)//עובר על רשימת התמונות
            {
                if (_filesManage.DeleteFile(img, "") == false)//שולחת את התמונה שנרצה למחוק
                {
                    countFalseTry++;
                }
            }

            if (countFalseTry > 0)//אם נכשלנו בביצוע של מחיקה אחת או יותר, נחזיר שגיאה
            {
                //לעורך, אין גישה לשרת ולא יוכל לתקן דבר. הבדיקה משמשת אותנו לצורך בדיקות
                return BadRequest("problem with " + countFalseTry.ToString() + " images");
            }
            return Ok("deleted");
        }
    }

}

