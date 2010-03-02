using System;
using System.Collections.Generic;

enum Mode {
	Chase,
	Hide
}

class MyTronBot {

   private const int TIME_LIMIT = 1000;
   private const int TIME_THRESHOLD = 150;
   private const int MAX_MAP_SIZE = 2500;

   private static DateTime lastTime;
   private static bool firstMove = true;

   private static Mode mode;

	// making these global to reduce garbage collection
   private static readonly Stack<GameState> toVisitStack = new Stack<GameState>();
   private static readonly Queue<GameState> toVisit = new Queue<GameState>();
   private static readonly List<GameState> visited = new List<GameState>();

   private static float GetEuclideanOpponentDistance(int x, int y)
   {
	return GetEuclideanOpponentDistance(new Point(x,y),new Point(Map.OpponentX(),Map.OpponentY()));
   }

   private static float GetEuclideanOpponentDistance(Point me, Point opponent)
   {
	return (float)Math.Abs(Math.Sqrt(Math.Pow((float)me.X - (float)opponent.X,2) + Math.Pow((float)me.Y - (float)opponent.Y,2)));
   }

   // This is junk from start package
   private static string PerformFoolishRandomMove()
   {
    int x = Map.MyX();
    int y = Map.MyY();
    List<string> validMoves = new List<string>();
    
    if (!Map.IsWall(x,y-1)) {
	    validMoves.Add("North");
	 }
	 if (!Map.IsWall(x+1,y)) {
	    validMoves.Add("East");
	 }
	 if (!Map.IsWall(x,y+1)) {
	    validMoves.Add("South");
	 }
	 if (!Map.IsWall(x-1,y)) {
	    validMoves.Add("West");
	 }
	 if (validMoves.Count == 0) {
	    return "North"; // Hopeless. Might as well go North!
	 } else {
	    Random rand = new Random();
	    int whichMove = rand.Next(validMoves.Count);
	    return validMoves[whichMove];
	 }
   }

   // Evaluation function for alpha-beta pruning / minimax
   private static float EvaluateMove(GameState gs)
   {
	if (gs.IsMyWin()) {
		return MAX_MAP_SIZE;
	}

	if (gs.IsOpponentWin()) {
		return -MAX_MAP_SIZE;
	}

	if (gs.IsDraw()) {
//		return 0
	}


	Territory room = new Territory(gs);
	room.DetermineTerritories();

	int mySize = room.GetMySize();
	int opponentSize = room.GetOpponentSize();
	int size = room.GetMySize() - room.GetOpponentSize();
	//Console.Error.WriteLine(String.Format("my room:{0} other room:{1}",mySize,opponentSize));	

	return (float)size;
   }

   private static float AlphaBeta(GameState gs, int depth, float alpha, float beta, bool isMax)
   {
	if (depth == 0 || gs.IsEndGame()) {
		float val = EvaluateMove(gs);
		return isMax ? val : -val;
	}

	Point p = null;
	if (isMax) {
		p = new Point(gs.MyX(), gs.MyY());
	} else {
		p = new Point(gs.OpponentX(), gs.OpponentY());
	}

	GameState newState = null;		
	foreach (Point child in gs.PossibleMoves(p.X, p.Y, true)) {
		if (isMax) {
			 newState = gs.ApplyMoveToMeAndCreate(child.GetDirectionFromPoint(p.X, p.Y));
		} else {
			 newState = gs.ApplyMoveToOpponentAndCreate(child.GetDirectionFromPoint(p.X, p.Y));		
		}

		alpha = Math.Max (alpha, -AlphaBeta(newState, depth-1, -beta, -alpha, !isMax));
		if (beta <= alpha) {
			break;
		}
	}

	return alpha;
   }

   private static int ScoreStraightPath(string direction)
   {
	return ScoreStraightPath(direction, new Point(Map.MyX(),Map.MyY()));
   }

   private static int ScoreStraightPath(string direction, Point p)
   {
	int score = 0;
	p.MoveInDirection(direction);

	while(!Map.IsWall(p.X, p.Y)) {
		score++;
		p.MoveInDirection(direction);
	}

	return score;
   }

   private static int BreadthFirst(GameState gs)
   {
	return BreadthFirst(gs, true);
   }

   private static int BreadthFirst(GameState gs, bool me)
   {
	//Queue<GameState> toVisit = new Queue<GameState>();
	//List<GameState> visited = new List<GameState>();
	toVisit.Clear();
	visited.Clear();
	toVisit.Enqueue(gs);

	while(toVisit.Count != 0) {
		GameState v = toVisit.Dequeue();
		if (!visited.Contains(v)) {
			visited.Add(v);
			if (me) {
				foreach (Point n in gs.PossibleMoves(v.MyX(), v.MyY())) {
					 toVisit.Enqueue(v.ApplyMoveToMeAndCreate(n.GetDirectionFromPoint(v.MyX(), v.MyY())));
				}
			} else {
				foreach (Point n in gs.PossibleMoves(v.OpponentX(), v.OpponentY())) {
					 toVisit.Enqueue(v.ApplyMoveToOpponentAndCreate(n.GetDirectionFromPoint(v.OpponentX(), v.OpponentY())));
				}
			}
		}
	}

	return visited.Count;
   }

   private static int FloodFill(GameState gs, int x, int y)
   {
	Queue<Point> q = new Queue<Point>();

	int total = 0;

	// shallow copy array
	bool[,] map = (bool[,])gs.map.Clone();

	q.Enqueue(new Point(x,y));
	map[x,y] = false;	
	//Console.Error.WriteLine(x + " " + y);

	while(q.Count > 0) {
		Point n = q.Dequeue();
		if (n.X < 0 || n.Y < 0 || n.X >= gs.Width() || n.Y >= gs.Height())
			continue;
	
		// process neighbours, mark as visited and increment count
		if (!map[n.X, n.Y]) {
			q.Enqueue(new Point(n.X+1, n.Y));
			q.Enqueue(new Point(n.X-1, n.Y));
			q.Enqueue(new Point(n.X, n.Y-1));
			q.Enqueue(new Point(n.X, n.Y+1));
			map[n.X,n.Y] = true;
			total++;
		}

	}
	
    return total;
   }

   private static int FloodFillDepthFirst(GameState gs)
   {
	return FloodFillDepthFirst(gs, true);
   }

   // do flood fill from all reachable nodes in graph and return running count
   private static int FloodFillDepthFirst(GameState gs, bool me)
   {
	toVisitStack.Clear();
	visited.Clear();
	toVisitStack.Push(gs);

	int score = 0;

	while(toVisitStack.Count != 0) {
		GameState v = toVisitStack.Pop();
		if (!visited.Contains(v)) {
			visited.Add(v);
			
			if (me) {
				score += FloodFill(v, v.MyX(), v.MyY());
				foreach (Point n in gs.PossibleMoves(v.MyX(), v.MyY())) {
					 toVisitStack.Push(v.ApplyMoveToMeAndCreate(n.GetDirectionFromPoint(v.MyX(), v.MyY())));
				}
			} else {
				score += FloodFill(v, v.OpponentX(), v.OpponentY());
				foreach (Point n in gs.PossibleMoves(v.OpponentX(), v.OpponentY())) {
					 toVisitStack.Push(v.ApplyMoveToOpponentAndCreate(n.GetDirectionFromPoint(v.OpponentX(), v.OpponentY())));
				}

			}

		}
	}

	return score;
   }

   // used with A*
   private static Path GetPath(GameState gs)
   {
	GameState parent;
    GameState current = gs;

	int length = 0;
	float cost = current.GetScore();

	while((parent = current.GetParent()) != null) {
		length++;
		cost += parent.GetScore();
		if (parent.MyX() == Map.MyX() && parent.MyY() == Map.MyY()) {
			//Console.Error.WriteLine("Got move");
			string direction = new Point(current.MyX(), current.MyY()).GetDirectionFromPoint(Map.MyX(), Map.MyY());
			return new Path(direction, length, cost);
		} else {
			current = parent;
		}
	}

	return null;
   }

   // Modified A* search
   // TODO clean up this is barely legible
   private static Path MoveByShortestPath(GameState gs, Point goal)
   {
	List<GameState> toVisit = new List<GameState>();
	List<GameState> visited = new List<GameState>();
	toVisit.Add(gs);

	while(toVisit.Count != 0) {
		// determine which node in queue is closet to the goal
		if (toVisit.Count > 1) {
			float best = GetEuclideanOpponentDistance(toVisit[0].MyX(), toVisit[0].MyY());
			float tmp;
			int bestIndex = 0;
			for(int i=1; i < toVisit.Count; i++) {
				tmp = GetEuclideanOpponentDistance(toVisit[i].MyX(), toVisit[i].MyY());
				if (tmp < best) { 
					bestIndex = i;
					best = tmp;
				}
			}
			if (bestIndex > 0) {
				GameState removed = toVisit[bestIndex];
				toVisit.RemoveAt(bestIndex);
				toVisit.Insert(0, removed);
			}
		}

		GameState v = toVisit[0];
		toVisit.RemoveAt(0);

		if (!visited.Contains(v)) {
			visited.Add(v);

			foreach (Point n in gs.PossibleMoves(v.MyX(), v.MyY(), true)) {

				// goal found
				if (goal != null && n.X == goal.X && n.Y == goal.Y) {
					//Console.Error.WriteLine("Found");
					GameState found = v.ApplyMoveToMeAndCreate(n.GetDirectionFromPoint(v.MyX(), v.MyY()));
					found.SetParent(v);
					return GetPath(found);
				
				// add neighbours to queue
				} else if (!v.IsWall(n.X, n.Y)) {
					GameState next = v.ApplyMoveToMeAndCreate(n.GetDirectionFromPoint(v.MyX(), v.MyY()));

					if (toVisit.Contains(next)) {
						GameState parent = toVisit[toVisit.IndexOf(next)].GetParent();
						// path back to start node is shorter from node being processed
						if (GetEuclideanOpponentDistance(new Point(v.MyX(), v.MyY()), new Point(Map.MyX(), Map.MyY())) < parent.GetScore()) {
							//Console.Error.WriteLine("betta");							
							toVisit[toVisit.IndexOf(next)].SetParent(v);
							continue;
						}
					}

					v.SetScore(GetEuclideanOpponentDistance(new Point(v.MyX(), v.MyY()), new Point(Map.MyX(), Map.MyY())));
					next.SetParent(v);
					toVisit.Add(next);
				}
			}
		}
	}

	return null;
   }

   private static Path PerformChaseMove()
   {
	GameState gs = new GameState();

	Path p = null;
	p = MoveByShortestPath(gs,new Point(Map.OpponentX(), Map.OpponentY()));

	return p;
   }

   // Determine which direction has the most available spaces and fill
   // as efficiently as possible
   private static string PerformSurvivalMove()
   {
	float score = 0;
	float bestScore = 0;
	int depth = 0;

	GameState gs = new GameState();	
	Point p = new Point();
	string bestMove = Map.MOVES[0];

	List<string> ties = new List<string>();

	for(int i=0; i<Map.MOVES.Length; i++) {
		p.X = Map.MyX(); p.Y = Map.MyY();
		p.MoveInDirection(Map.MOVES[i]);
		if (!Map.IsWall(p.X, p.Y)) {
			//score = FloodFill(gs.ApplyMoveToMeAndCreate(Map.MOVES[i]), p.X, p.Y);
			score = FloodFillDepthFirst(gs.ApplyMoveToMeAndCreate(Map.MOVES[i]));
		} else {
			score = 0;
		}
		//Console.Error.WriteLine("Far:" + Map.MOVES[i] + ":" + score);
		if (score > bestScore) {
			bestMove = Map.MOVES[i];
			bestScore = score;
			ties.Clear();
			ties.Add(bestMove);
		} else if (score == bestScore) {
			ties.Add(Map.MOVES[i]);
		}
	}

	// break ties
	// hug closest wall
	if (ties.Count > 0) {
		bestScore = int.MaxValue;
		foreach(string move in ties) {
			p.X = Map.MyX(); p.Y = Map.MyY();
			p.MoveInDirection(move);
			// use shortest distance to closest wall
			score = int.MaxValue;
			int tmp;
			foreach (string direction in Map.MOVES) {
				Point q = new Point(p.X, p.Y);
				q.MoveInDirection(direction);
				if (q.X == Map.MyX() && q.Y == Map.MyY()) {
					continue;
				}
				q.X = p.X; 
				q.Y = p.Y;
				tmp = ScoreStraightPath(direction, q);
				if (tmp < score) {
					score = tmp;
				}
			}
			//Console.Error.WriteLine("Far tie break:" + move + ":" + score);
			if (score < bestScore) {
				bestScore = score;
				bestMove = move;
			}
		}
	}
		
	return bestMove;
   }

   // Determine which direction has the most available spaces and fill
   // as efficiently as possible
   // TODO come back to this maybe...it's TOO SLOW!
/*
   private static string PerformSurvivalMove()
   {
	float score = 0;
	float bestScore = 0;
	int depth = 0;


	GameState gs = new GameState();	
	int breadth = BreadthFirst(gs);
	int maxDepth = (int)((double)breadth / Math.Pow(2, (double)breadth / 20));
	Console.Error.WriteLine("breadth " + breadth + " " + maxDepth);

	Point p = new Point();
	string bestMove = Map.MOVES[0];

	List<string> ties = new List<string>();

	Queue<GameState> previous = new Queue<GameState>();
	List<GameState> paths = new List<GameState>();
	previous.Enqueue(gs);

	int lastLength = 0;

	while(previous.Count > 0 && (depth < 10 && depth < maxDepth)) {
		GameState basis = previous.Dequeue();

		for(int i=0; i<Map.MOVES.Length; i++) {
			p.X = basis.MyX(); p.Y = basis.MyY();
			p.MoveInDirection(Map.MOVES[i]);
			if (!Map.IsWall(p.X, p.Y)) {
				GameState move = basis.ApplyMoveToMeAndCreate(Map.MOVES[i]);
				score = FloodFillDepthFirst(move);
				move.SetScore(score);
				move.SetParent(basis);
				previous.Enqueue(move);
				paths.Add(move);
			//Console.Error.WriteLine("Far:" + Map.MOVES[i] + ":" + score);
			}
		}

		Path path = null;
		foreach (GameState move in paths) {
			path = GetPath(move);
			//Console.Error.WriteLine("Survive:" + path.direction + ":" + path.cost + " length:" + path.length);
			if (path.cost > bestScore) {
				bestScore = path.cost;
				bestMove = path.direction;
			}
		}

		if (path != null && path.length > lastLength) {
			lastLength = path.length;
			paths.Clear();
			bestScore = 0;
			depth++;
		}
	}

	// break ties
	// hug closest wall
	if (ties.Count > 0) {
		bestScore = int.MaxValue;
		foreach(string move in ties) {
			p.X = Map.MyX(); p.Y = Map.MyY();
			p.MoveInDirection(move);
			// use shortest distance to closest wall
			score = int.MaxValue;
			int tmp;
			foreach (string direction in Map.MOVES) {
				Point q = new Point(p.X, p.Y);
				q.MoveInDirection(direction);
				if (q.X == Map.MyX() && q.Y == Map.MyY()) {
					continue;
				}
				q.X = p.X; 
				q.Y = p.Y;
				tmp = ScoreStraightPath(direction, q);
				if (tmp < score) {
					score = tmp;
				}
			}
			//Console.Error.WriteLine("Far tie break:" + move + ":" + score);
			if (score < bestScore) {
				bestScore = score;
				bestMove = move;
			}
		}
	}
		
	return bestMove;
   }
*/

   private static string PerformFarMove()
   {
	return PerformFarMove(null);
   }

   // Determine which direction has the most available spaces 
   // NOT USED
   private static string PerformFarMove(Path shortestPath)
   {
	float score = 0;
	float bestScore = 0;

	GameState gs = new GameState();	
	Point p = new Point();
	string bestMove = Map.MOVES[0];

	List<string> ties = new List<string>();

	for(int i=0; i<Map.MOVES.Length; i++) {
		p.X = Map.MyX(); p.Y = Map.MyY();
		p.MoveInDirection(Map.MOVES[i]);
		if (!Map.IsWall(p.X, p.Y)) {
			score = BreadthFirst(gs.ApplyMoveToMeAndCreate(Map.MOVES[i]));
		} else {
			score = 0;
		}
		//Console.Error.WriteLine("Far:" + Map.MOVES[i] + ":" + score + " bestscore:" + bestScore);
		if (score > bestScore) {
			bestMove = Map.MOVES[i];
			bestScore = score;
			ties.Clear();
			ties.Add(bestMove);
		} else if (score == bestScore) {
			ties.Add(Map.MOVES[i]);
		}
	}

	// break ties
	if (ties.Count > 1) {
		if (shortestPath != null) {
			return shortestPath.direction;
		}
		bestScore = int.MaxValue;
		foreach(string move in ties) {
			p.X = Map.MyX(); p.Y = Map.MyY();
			p.MoveInDirection(move);
			if (shortestPath == null) {
				// use shortest distance to closest wall
				score = int.MaxValue;
				int tmp;
				foreach (string direction in Map.MOVES) {
					Point q = new Point(p.X, p.Y);
					q.MoveInDirection(direction);
					if (q.X == Map.MyX() && q.Y == Map.MyY()) {
						continue;
					}
					q.X = p.X; 
					q.Y = p.Y;
					tmp = ScoreStraightPath(direction, q);
					if (tmp < score) {
						score = tmp;
					}
				}
			}
			//Console.Error.WriteLine("Far tie break:" + move + ":" + score);
			if (score < bestScore) {
				bestScore = score;
				bestMove = move;
			}
		}
	}
		
	return bestMove;
   }

   // alpha beta with iterative deepening
   private static string PerformNearMove(Path shortestPath)
   {
	int depth = 0;
	float time = 0;
	float score, bestScore;
	GameState gs = new GameState();	
	Point p = new Point();
	// default to something that won't kill us - server sometimes
	// runs out of time WAY early resulting in no time to perform alpha beta
	// iterations
	string bestMove = Map.MOVES[0]; //PerformFoolishRandomMove();
	DateTime lastAlphaBeta;

	List<string> ties = new List<string>();

	string[] moves = new string[4];
	float[] scores = new float[4];	// 0 north, 1 south, 2 east, 3 west
	scores[0]=3;		
	scores[1]=2;
	scores[2]=1;
	scores[3]=0;	

	// used to adjust time estimate for next depth so we don't go over time limit
	float timebase = ((float)Map.Width() * (float)Map.Height()) / (15f * 15f);

	while(Duration() + time < (TIME_LIMIT - TIME_THRESHOLD) && depth <= 12) {
		score = int.MinValue;
		bestScore = int.MinValue;
		depth++;

		// order moves by previous iterations scores
		// TODO this really does nothing. Cache of game states is needed
		// for quick eval retrival and move ordering
		int length = scores.Length;
		bool swapped = true;

		moves[0] = "North";
		moves[1] = "South";
		moves[2] = "East";
		moves[3] = "West";

		while(swapped) {
			swapped = false;
			for(int b=0; b<length - 1; b++) {
				if (scores[b] < scores[b+1]) {
					string tmp = moves[b];
					float ftmp = scores[b];

					moves[b] = moves[b+1];
					scores[b] = scores[b+1];

					moves[b+1] = tmp;
					scores[b+1] = ftmp;

					swapped = true;
				}
			}
			length -= 1;

			//Console.Error.WriteLine("best:" + best + " score:" + scores[best]);
		}

		for(int i=0; i<moves.Length; i++) {
			string move = moves[i];
			p.X = Map.MyX(); p.Y = Map.MyY();
			p.MoveInDirection(move);
			if (!Map.IsWall(p.X, p.Y)) {
				// negate since starting with opponents moves
				lastAlphaBeta = DateTime.Now;
				score = -AlphaBeta(gs.ApplyMoveToMeAndCreate(move), depth, -int.MaxValue, -(-int.MaxValue), false);
				
				// estimate time for next depth
				TimeSpan ts = DateTime.Now - lastAlphaBeta;
				time = (float)ts.Milliseconds * (depth * timebase);
			} else {
				score = int.MinValue;
			}
			//Console.Error.WriteLine("alphabeta:" + move + ":" + score + " depth:" + depth);
			if (score > bestScore) {
				bestMove = move;
				bestScore = score;
				ties.Clear();
				ties.Add(bestMove);
			} else if (score == bestScore) {
				ties.Add(move);
			}
			
			// track score
			string temp = move.Substring(0, 1).ToUpper(); 
		    int firstChar = (int)temp[0];
			switch(firstChar) {
			case 'N':
				scores[0] = score;
				break;
			case 'S':
				scores[1] = score;
				break;
			case 'E':
				scores[2] = score;	
				break;
			case 'W':
				scores[3] = score;
				break;
			}
		}
		depth++;
	}

	List<string> secondaryTies = new List<string>();
	// break ties
	if (ties.Count > 1) {
		bestScore = int.MinValue;
		foreach(string move in ties) {
			//Console.Error.WriteLine("alpha tie break:" + move);
			p.X = Map.MyX(); p.Y = Map.MyY();
			p.MoveInDirection(move);
			if (Map.IsWall(p.X, p.Y)) {
				continue;
			}

			Territory room = new Territory(gs.ApplyMoveToMeAndCreate(move));
			room.DetermineTerritories();
			score = (float)room.GetMySize() - (float)room.GetOpponentSize();
	
			//Console.Error.WriteLine("alpha tie break:" + move + ":" + score);
			if (score > bestScore) {
				bestScore = score;
				bestMove = move;
				secondaryTies.Clear();
				secondaryTies.Add(move);
			} else if (score == bestScore) {
				secondaryTies.Add(move);
			}
		}
	}

	// kinda lame, but need another tie breaker...quick and dirty
	if (secondaryTies.Count > 1) {
		bestScore = int.MinValue;
		foreach(string move in ties) {
			if (shortestPath != null) {
				if (move.Equals(shortestPath.direction)) {
					bestMove = shortestPath.direction;
					break;
				}
			}
			p.X = Map.MyX(); p.Y = Map.MyY();
			p.MoveInDirection(move);
			if (Map.IsWall(p.X, p.Y)) {
				continue;
			}
			score = -GetEuclideanOpponentDistance(p.X, p.Y);
			//Console.Error.WriteLine("alpha tie break:" + move + ":" + score);
			if (score > bestScore) {
				bestScore = score;
				bestMove = move;
			}
		}
	}

	return bestMove;
   }

   public static string MakeMove()
   {
	String move = null;
	int width = Map.Width();
	int height = Map.Height();
	Path path = PerformChaseMove();

	// means our enemy is attainable
	if (path != null) {
		move = path.direction;
		//if (path.length < (width + height)) {
			move = PerformNearMove(path);
		//} else {
			//move = PerformFarMove(path);
		//}
	} else if (path == null) {
		move = PerformSurvivalMove();
	}

	return move;
   }
   
   private static long Duration()
   {
	TimeSpan ts = DateTime.Now - lastTime;
	return ts.Milliseconds;
   }

   public static void Main()
   {
    while (true) {
	    Map.Initialize();
		lastTime = DateTime.Now;
		if (firstMove) {
			firstMove = false;
			mode = Map.DetermineMode();
		}
	    Map.MakeMove(MakeMove());
		Console.Error.WriteLine(Duration());
	 }
   }
}


       

