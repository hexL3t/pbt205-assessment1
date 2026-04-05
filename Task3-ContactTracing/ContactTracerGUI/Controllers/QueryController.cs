using Microsoft.AspNetCore.Mvc;
using ContactTracerGui.Services;

namespace ContactTracerGui.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly PositionListenerService _service;

        public QueryController(PositionListenerService service)
        {
            _service = service;
        }

        [HttpGet("{name}")]
        public IActionResult GetContacts(string name)
        {
            var contacts = _service.GetContactsFor(name);
            return Ok(contacts);
        }
    }
}
