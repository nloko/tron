using System;
using System.Collections.Generic;

sealed class Territory
{
	private GameState gs;

	private Point me;
	private Point you;

    private int[,] map;
	private int mySize = 0;
	private int opponentSize = 0;

	private const int BOUNDARY = 9999;

	public Territory(GameState gs)
	{
		this.gs = gs;
		this.me = new Point(gs.MyX(), gs.MyY());
		this.you = new Point(gs.OpponentX(), gs.OpponentY());
	}

	// modified flood fill to scan outwards from each players' locations
	// and mark their territories
	private void scan()
	{
		map = new int[gs.Width(),gs.Height()];
	
		Queue<Point> q = new Queue<Point>();
		q.Enqueue(me);
		q.Enqueue(you);

		map[me.X,me.Y] = 1;
		map[you.X,you.Y] = -1;

		int player = 1;
		int level = 1;

		while(q.Count > 0) {
			Point node = q.Dequeue();
			if (node.X < 0 || node.Y < 0 || node.X >= gs.Width() || node.Y >= gs.Height())
				continue;
			// exclude first nodes from this check
			if (Math.Abs(map[node.X, node.Y]) != 1 && gs.IsWall(node.X, node.Y))
				continue;
			

			level = map[node.X, node.Y];
			// already marked as BOUNDARY so skip
			if (level == BOUNDARY) 
				continue;

			// check player: + us - opponent
			if (level > 0) {
				player = 1;
			} else if (level < 0) {
				player = -1;
			}

			// bump level
			level = Math.Abs(level) + 1;

			//Console.Error.WriteLine("x " + node.X + " y " + node.Y + " value " + map[node.X, node.Y]);

			// process the neighbours
			Point north = new Point(node.X, node.Y-1);
			if (ProcessNeighbour(north, level, player))
				q.Enqueue(north);

			Point east = new Point(node.X+1, node.Y);
			if (ProcessNeighbour(east, level, player))
				q.Enqueue(east);

			Point south = new Point(node.X, node.Y+1);
			if (ProcessNeighbour(south, level, player))
				q.Enqueue(south);

			Point west = new Point(node.X-1, node.Y);
			if (ProcessNeighbour(west, level, player))
				q.Enqueue(west);

		}

	}

    private bool ProcessNeighbour(Point node, int level, int player)
	{
		if (node.X < 0 || node.Y < 0 || node.X >= gs.Width() || node.Y >= gs.Height())
			return false;
		
		if (gs.IsWall(node.X, node.Y))
			return false;

		int val = map[node.X, node.Y];
		if (Math.Abs(val) == 1)
			return false;

		// already processed by other player and levels are equal so define boundary 
		// of territory and do not process its neighbours
		if (val != 0 && val != BOUNDARY && val * player < 0 && Math.Abs(val) == level) {
			map[node.X, node.Y] = BOUNDARY;
			// adjust size when area is marked as boundary
			if (player > 0) {
				opponentSize--;
			} else {
				mySize--;
			}
			return false;
		// unprocessed and our terrirtory. mark and visit neighbours
		} else if (val == 0 && player > 0) {
			map[node.X, node.Y] = level;
			mySize++;
			return true;
		// unprocessed and opponent territory. mark and visit neighbours
		} else if (val == 0 && player < 0) {
			map[node.X, node.Y] = -level;
			opponentSize++;
			return true;
		}

		// otherwise, do not visit neighbours
		return false;
	}

	public int GetMySize()
	{
		return mySize;
	}

	public int GetOpponentSize()
	{
		return opponentSize;
	}

	public void DetermineTerritories()
	{
		scan();
	}
}
