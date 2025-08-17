using System.ComponentModel.DataAnnotations;

namespace DynamicConfiguration.WebUI.Models
{
    public class ConfigurationViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Name alanı zorunludur")]
        [StringLength(100, ErrorMessage = "Name en fazla 100 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type alanı zorunludur")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Value alanı zorunludur")]
        public string Value { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Application Name alanı zorunludur")]
        [StringLength(50, ErrorMessage = "Application Name en fazla 50 karakter olabilir")]
        public string ApplicationName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ConfigurationListViewModel
    {
        public List<ConfigurationViewModel> Configurations { get; set; } = new();
        public string? FilterName { get; set; }
        public string? FilterApplication { get; set; }
        public List<string> Applications { get; set; } = new();
    }

    public static class ConfigurationTypes
    {
        public static readonly Dictionary<string, string> Types = new()
        {
            { "string", "String" },
            { "int", "Integer" },
            { "bool", "Boolean" },
            { "double", "Double" },
            { "decimal", "Decimal" },
            { "datetime", "DateTime" }
        };
    }
}