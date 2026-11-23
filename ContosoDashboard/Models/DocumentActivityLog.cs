using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivityLog
{
    [Key]
    public int DocumentActivityId { get; set; }

    public int? DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public bool Success { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document? Document { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public virtual User Actor { get; set; } = null!;
}
