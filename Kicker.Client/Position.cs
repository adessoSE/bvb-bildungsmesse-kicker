namespace Kicker.Client;

public record Position(int Column, int Row)
{
    public bool IsAbove(Position other) => Row > other.Row;
    public bool IsBelow(Position other) => Row < other.Row;
    public bool IsRightOf(Position other) => Column > other.Column;
    public bool IsLeftOf(Position other) => Column < other.Column;
}