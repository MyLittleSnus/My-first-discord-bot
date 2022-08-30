using System.ComponentModel.DataAnnotations;

namespace BotConfiguration.Entities
{
    public class Participant
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(20)]
        public string Name { get; set; }
        [MaxLength(20)]
        public string Surname { get; set; }
        [MaxLength(20)]
        public string? Username { get; set; }
        [MaxLength(20)]
        public string? CurrentCountry { get; set; }

        public DateTime? BirthdayDate { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is Participant)
            {
                var otherParticipant = obj as Participant;

                if (otherParticipant.Name == this.Name &&
                    otherParticipant.Surname == this.Surname)
                    return true;
            }

            return false;
        }
    }
}