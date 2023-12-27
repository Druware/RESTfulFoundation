using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RESTfulFoundation.UnitTest.Server.Entities;
using RESTfulFoundation.Server;

namespace RESTfulFoundation.UnitTest.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly EFContext _context;
        public PlayersController(EFContext context) => _context = context;
        
        // GET: api/Players
        [HttpGet]
        public async Task<IActionResult> GetPlayers(
            [FromQuery] int? page = null,
            [FromQuery] int? perPage = null)
        {
            // this could return either a ListResult ( if there is a page/perPage option passed in )
            if (page != null && perPage != null)
            {
                return Ok(ListResult.Ok(await _context.Players
                    .Skip((int)(page! * perPage!))
                    .Take((int)perPage!)
                    .ToListAsync(),
                    (await _context.Players.ToListAsync()).Count,
                    page, perPage));
            }

            return Ok(await _context.Players.ToListAsync());
        }

        // GET: api/Players/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Player>> GetPlayer(long id)
        {
            var player = await _context.Players.FindAsync(id);

            if (player == null)
            {
                return NotFound();
            }

            return player;
        }

        // PUT: api/Players/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlayer(long id, Player player)
        {
            if (id != player.PlayerId)
            {
                return BadRequest();
            }

            _context.Entry(player).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlayerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Players
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Player>> PostPlayer(Player player)
        {
            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPlayer", new { id = player.PlayerId }, player);
        }

        // DELETE: api/Players/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlayer(long id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            // provide a bit more context...
            // return NoContent();
            List<string> info = new();
            info.Add("Delete Successful");
            return Ok(Result.Ok(info));
        }

        private bool PlayerExists(long id)
        {
            return _context.Players.Any(e => e.PlayerId == id);
        }
    }
}
