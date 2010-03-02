using System;

sealed class Path
{
	public string direction;
	public int length;
	public float cost;

	public Path(string direction, int length) : this(direction, length, 0)
	{
	}

	public Path(string direction, int length, float cost)
    {
		this.direction = direction;
		this.length = length;
		this.cost = cost;
	}
}
