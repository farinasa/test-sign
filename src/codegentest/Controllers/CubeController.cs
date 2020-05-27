using Microsoft.AspNetCore.Mvc;
using codegentest.Services;

namespace codegentest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CubeController : ControllerBase
    {
        // GET api/cube/5
        [HttpGet("{value}")]
        public ActionResult<int> Get(int value)
        {
            return CubeService.Cube(value);
        }
    }
}
