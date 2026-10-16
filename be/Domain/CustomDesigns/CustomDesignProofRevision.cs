using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.CustomDesigns;
public class CustomDesignProofRevision
{
    [Key]
    public int Id { get; set; }
    public int CustomDesignId { get; set; }
    public int AssetId { get; set; }
    public int RevisionNumber { get; set; }
    public ProofRevisionStatus Status { get; set; } = ProofRevisionStatus.AwaitingApproval;

    [StringLength(1000)]
    public string? CustomerVisibleMessage { get; set; }

    [StringLength(1000)]
    public string? CustomerResponse { get; set; }
    public DateTime? CustomerResponseAt { get; set; }
    public string? UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CustomDesignId))]
    public CustomDesign? CustomDesign { get; set; }

    [ForeignKey(nameof(AssetId))]
    public CustomDesignAsset? Asset { get; set; }
}
