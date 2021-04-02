using System;

namespace Models.Entities
{
    public class PlaceOfWork
    {
        public Guid Id { get; set; }
        public string Position { get; set; }
        public string Company { get; set; }
        public string Description { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public Guid ResumeId { get; set; }
    }
}