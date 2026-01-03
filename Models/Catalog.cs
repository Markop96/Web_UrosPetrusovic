namespace UrosPetrusovic.Models
{
    using System;

    public class Catalog
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime? ValidUntil { get; set; }
    }

}
