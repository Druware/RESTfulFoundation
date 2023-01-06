using System.ComponentModel.DataAnnotations;

namespace RESTfulFoundation.UnitTest.Server.Entities
{
    public class Player
    {
        [Key]
        public long PlayerId { get; set; }
        public string? PlayerName { get; set; }
    }
}
