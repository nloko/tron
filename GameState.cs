using System;
using System.Collections.Generic;

sealed class GameState
{
   internal bool[,] map;
     // Stores the locations of the two players.
   private Point myLocation, opponentLocation;

   private int width, height; 
   private bool myWin = false;
   private bool opponentWin = false;

   private GameState parent;
   private float score;

   public GameState() : this(Map.GetWall(), Map.Width(), Map.Height(), new Point(Map.MyX(), Map.MyY()), new Point(Map.OpponentX(), Map.OpponentY()))
   {
   }

   public GameState(GameState gs) : this(gs.map, gs.Width(), gs.Height(), new Point(gs.MyX(), gs.MyY()), new Point(gs.OpponentX(), gs.OpponentY()))
   {
	this.map = (bool[,])gs.map.Clone();
   }

   private GameState(bool[,] map, int width, int height, Point me, Point opponent)
   {
	this.map = map;
	this.width = width;
	this.height = height;
	myLocation = me;
	opponentLocation = opponent;
   }

   // TODO this is really not good enough 
   // should be including walls too
   // 
   public override bool Equals(Object obj)
   {
	if (obj == null) {
		return false;
	}

	GameState key = (GameState)obj;
	if (key == null) {
		return false;
	}

	return key.MyX() == MyX() && key.MyY() == MyY() &&
		key.OpponentX() == OpponentX() && key.OpponentY() == OpponentY();
   }

   public override int GetHashCode()
   {
	return MyX().GetHashCode() + 
		MyY().GetHashCode() + 
		OpponentX().GetHashCode() + 
		OpponentY().GetHashCode();
   }

  public float GetScore()
  {
	return score;
  }

  public void SetScore(float score)
  {
	this.score = score;
  }

  public void SetParent(GameState gs)
  {
	parent = gs;
  }

  public GameState GetParent()
  {
	return parent;
  }

  public int Width() 
  {
	return width;
  }

  public int Height()
  {
	return height;
  }


  public bool IsWall(int x, int y) 
  {
	if (x < 0 || y < 0 || x >= width || y >= height) 
	{
	    return true;
	} 
	else 
	{
	    return map[ x, y ];
	}
  }

  // My X location.
  public  int MyX() 
  {
	    return (int)myLocation.X;
  }

  // My Y location.
  public  int MyY() 
  {
	    return (int)myLocation.Y;
  }  
  
  // The opponent's X location.
  public  int OpponentX() 
  {
	    return (int)opponentLocation.X;
  }

  // The opponent's Y location.
  public  int OpponentY() 
  {
	    return (int)opponentLocation.Y;
  }

  public bool IsDraw()
  {
	return MyX() == OpponentX() && MyY() == OpponentY();
  }

  public bool IsOpponentWin()
  {
	return !IsDraw() && opponentWin;
  }

  public bool IsMyWin()
  {
	return !IsDraw() && myWin;
  }

  public bool IsEndGame()
  {
	return IsDraw() || IsMyWin() || IsOpponentWin();
  }

  public void ApplyMoveToMe(string direction)
  {
//	Console.Error.WriteLine("Applying  move to me:" + direction);
	//Console.Error.WriteLine(String.Format("Before X:{0} Y:{1}, MapX:{2} MapY:{3}",MyX(), MyY(),Map.MyX(), Map.MyY()));
    myLocation.MoveInDirection(direction);
	if (map[myLocation.X, myLocation.Y]) {
		opponentWin = true;
	} else {
		map[myLocation.X, myLocation.Y] = true;
	}
	//Console.Error.WriteLine(String.Format("After X:{0} Y:{1}, MapX:{2} MapY:{3}",MyX(), MyY(),Map.MyX(), Map.MyY()));
  }

  public void ApplyMoveToOpponent(string direction)
  {
//	Console.Error.WriteLine("Applying  move to opponent:" + direction);
	opponentLocation.MoveInDirection(direction);
    if (map[opponentLocation.X, opponentLocation.Y]) {
		myWin = true;
	} else {
		map[opponentLocation.X, opponentLocation.Y] = true;
	}
  }

  public GameState ApplyMoveToMeAndCreate(string direction)
  {
    GameState gs = new GameState((bool[,])map.Clone(), 
		width, 
		height, 
		new Point(myLocation.X, myLocation.Y), 
		new Point(opponentLocation.X, opponentLocation.Y));
	gs.ApplyMoveToMe(direction);
	return gs;
  }

  public GameState ApplyMoveToOpponentAndCreate(string direction)
  {
	GameState gs = new GameState((bool[,])map.Clone(), 
		width, 
		height, 
		new Point(myLocation.X, myLocation.Y), 
		new Point(opponentLocation.X, opponentLocation.Y));
	gs.ApplyMoveToOpponent(direction);
	return gs;
  }

   public IEnumerable<Point> PossibleMoves(int x, int y)
   {
	return PossibleMoves(x, y, false);
   }

   public IEnumerable<Point> PossibleMoves(int x, int y, bool ignoreWalls)
   {
	Point move = new Point();
	for(int i=0; i<Map.MOVES.Length; i++) {
		move.X = x; move.Y = y;
		move.MoveInDirection(Map.MOVES[i]);
		if (!ignoreWalls && !IsWall(move.X, move.Y)) {
			yield return move;
		} else if (ignoreWalls) {
			yield return move;
		}
	}
   }   
}
