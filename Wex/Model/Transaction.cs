using System.ComponentModel.DataAnnotations;

namespace Wex.Model;

public class Transaction
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    [Required, MaxLength(255)]
    public string Description { get; set; } = "";
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
}