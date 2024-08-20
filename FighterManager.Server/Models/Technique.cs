﻿using System.ComponentModel.DataAnnotations;

namespace FighterManager.Server.Models
{
    public class Technique
    {
        [Key]
        public int Id { get; set; }

        public string? Description { get; set; }
        public string? StartingPosition { get; set; }
        public string? DemoVideoLink { get; set; }
    }
}