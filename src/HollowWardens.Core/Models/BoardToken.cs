namespace HollowWardens.Core.Models;

public abstract class BoardToken
{
    public TokenType Type { get; set; }
    public int? Hp { get; set; }
    public string TerritoryId { get; set; } = string.Empty;
}

public class Infrastructure : BoardToken
{
    public Infrastructure()
    {
        Type = TokenType.Infrastructure;
        Hp = 2;
    }
}
