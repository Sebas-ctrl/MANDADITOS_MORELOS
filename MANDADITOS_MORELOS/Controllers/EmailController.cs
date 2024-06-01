using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;
using System.Net.Mail;
using System.Net;

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        // POST: api/Email
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Boolean>> PostEmail(String email)
        {
            var client = new SmtpClient("sandbox.smtp.mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("a78883aeeecc97", "56be617b37b205"),
                EnableSsl = true
            };
            client.Send("shadowtic9@gmail.com", email, "Hola", "testbody");
            if(client.Credentials != null)
            {
                return true;
            }
            return false;
        }
    }
}
