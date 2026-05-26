using Microsoft.EntityFrameworkCore;

namespace Wex.Model;

public class Card
{
    public int Id { get; set; }
    [Precision(18, 2)]
    public decimal LimitAmount { get; set; }
}
