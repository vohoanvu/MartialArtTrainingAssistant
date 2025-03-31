using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Models
{
    public class BeltRank
    {
        [Key]
        public int Id { get; set; }

        public required BeltColor Color { get; set; }

        [Range(0, 4)]
        public required int Stripe { get; set; }

        public List<Fighter>? Fighters { get; set; }
    }
}
