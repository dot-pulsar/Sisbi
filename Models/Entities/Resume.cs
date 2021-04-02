using System;

namespace Models.Entities
{
    public class Resume
    {
        public Guid Id { get; set; }
        public string Position { get; set; }
        public long Salary { get; set; }
        public Guid CityId { get; set; }
        public string Schedule { get; set; }
        public string Description { get; set; }
        public Guid UserId { get; set; }
    }
}