using System.ComponentModel.DataAnnotations;

namespace DataCopilot.Vectorize.Utils
{
    public class QueryModel
    {
        [Required]
        [StringLength(8000, ErrorMessage = "Identifier too long (8000 character limit).")]
        public string QueryText { get; set; } = "";

        [Required, Range(1, 5)]
        public int ResultsToShow { get; set; } = 30;

        public bool ResetContext { get; set; } = true;
    }
}
