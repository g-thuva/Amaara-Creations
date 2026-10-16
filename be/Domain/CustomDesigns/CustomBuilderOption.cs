using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.CustomDesigns;
public class CustomBuilderOption
{
    [Key]
    public int Id { get; set; }
    public int ConfigurationVersionId { get; set; }
    public BuilderOptionGroup Group { get; set; }

    [Required, StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Label { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPriceAdjustment { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    [ForeignKey(nameof(ConfigurationVersionId))]
    public CustomBuilderConfigurationVersion? ConfigurationVersion { get; set; }
}
