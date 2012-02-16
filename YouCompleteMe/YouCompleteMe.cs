using Sifteo;
using System;

namespace YouCompleteMe
{
	public class YouCompleteMe : BaseApp
	{

		override public int FrameRate {
			get { return 20; }
		}
		
		//actually arranged as:
		//0 1
		//2 3
		Cube[] cubes = new Cube[4];
		int textDelay = 6000;//6 seconds
		System.Diagnostics.Stopwatch textStopwatch = new System.Diagnostics.Stopwatch();
		int numCubesArranged = 0;
		string heartChar = "a";

		// called during intitialization, before the game has started to run
		override public void Setup ()
		{
			Log.Debug ("Setup()");
			foreach (Cube c in CubeSet) {
				c.NeighborAddEvent += HandleNeighborAddEvent;
				c.NeighborRemoveEvent += HandleNeighborRemoveEvent;
				c.FillScreen (Color.White);
				c.Paint ();
				//if (!c.Neighbors.IsEmpty){
				//	if (c.Neighbors.Right != null)
				//		HandleNeighborAddEvent(c, Cube.Side.RIGHT, c.Neighbors.Right, 
			}
		}

		override public void Tick ()
		{
			if(numCubesArranged == 3 && textStopwatch.IsRunning && textStopwatch.ElapsedMilliseconds > textDelay){
				displayText();
				textStopwatch.Reset ();
			}
		}
		
		void displayText(){
			int textIndex = 0;
			foreach(Cube c in cubes){
				if(c != null){
					c.FillScreen (new Color(100, 100, 255));
					c.Image ("text_"+textIndex++, 0, 0, 0, 0, Cube.SCREEN_WIDTH, Cube.SCREEN_HEIGHT, 1, 0);
					c.Paint ();
				}
			}
			Log.Debug ("printed text");
		}

		// development mode only
		// start YouCompleteMe as an executable and run it, waiting for Siftrunner to connect
		static void Main (string[] args)
		{
			new YouCompleteMe ().Run ();
		}
		
		/// <summary>
		/// Gets the primary cube.
		/// </summary>
		/// <returns>
		/// The index of the primary cube. This is the cube with the lowest index
		/// that is preferably not the Cube or the Neighbor.
		/// </returns>
		int GetPrimaryCube(Cube c, Cube neighbor){
			int currCube = -1;
			for(int i = 0; i < cubes.Length; i++){
				if (cubes[i] != null){
					if(cubes[i] != c && cubes[i] != neighbor){
						currCube = i;
						break;
					} else if (currCube == -1){
						currCube = i;
					}
				}
			}
			return currCube;
		}
		
		
		Cube FindVertical(Cube c){
			int i = IsCubeInArray (c);
			if (i >= 2){//I am on the bottom
				if (c.Neighbors.Top != null){// && c.Neighbors.Top.Neighbors.Bottom == c){
					c.Neighbors.Top.OrientTo (c);
					AddCubeToArray (c.Neighbors.Top, i & 1);
					return c.Neighbors.Top;
				}
			} else {//I am on the top
				if (c.Neighbors.Bottom != null){// && c.Neighbors.Bottom.Neighbors.Top == c){
					c.Neighbors.Bottom.OrientTo (c);
					AddCubeToArray (c.Neighbors.Bottom, i | 2);
					return c.Neighbors.Bottom;
				}
			}
			return null;
		}
		
		Cube FindHorizontal(Cube c){
			int i = IsCubeInArray (c);
			if ((i & 1) == 0){//I am on the left
				if (c.Neighbors.Right != null){// && c.Neighbors.Right.Neighbors.Left == c){
					c.Neighbors.Right.OrientTo (c);
					AddCubeToArray (c.Neighbors.Right, i + 1);
					return c.Neighbors.Right;
				}
			} else {//I am on the right
				if (c.Neighbors.Left != null){// && c.Neighbors.Left.Neighbors.Right == c){
					c.Neighbors.Left.OrientTo (c);
					AddCubeToArray (c.Neighbors.Left, i - 1);
					return c.Neighbors.Left;
				}
			}
			return null;
		}
		
		int addPrimary(Cube a, Cube b){
			if (a.Neighbors.Right != null || a.Neighbors.Bottom != null)
				AddCubeToArray (a,0);
			else
				AddCubeToArray(b,0);
			return 0;
		}
		
		void updateCubes(Cube c, Cube neighbor){
			int primary = GetPrimaryCube (c,neighbor);
			if (primary == -1){//if there are no cubes in the Array
				primary = addPrimary(c,neighbor);
			}
			Cube[] active = new Cube[4];
			active[0] = cubes[primary];
			active[1] = FindHorizontal (cubes[primary]);
			active[2] = FindVertical (cubes[primary]);
			active[3] = (active[1] != null 
			             ? FindVertical (active[1]) 
			             : (active[2] != null 
			             	? FindHorizontal (active[2]) 
			             	: null));
			bool found;
			numCubesArranged = 0;
			foreach(Cube sc in CubeSet){
				found = false;
				foreach(Cube ac in active){
					if (sc == ac){
						found = true;
						numCubesArranged++;
						break;
					}
				}
				if (!found){
					RemoveCubeFromArray (sc);
				}
			}
			if (numCubesArranged == 3){
				textStopwatch.Reset ();
				textStopwatch.Start ();
			} else {
				textStopwatch.Stop ();
			}
			PrintCubeArray();
		}

		void HandleNeighborRemoveEvent (Cube c, Cube.Side side, Cube neighbor, Cube.Side neighborSide)
		{
			updateCubes (c, neighbor);
		}
		
		void HandleNeighborAddEvent (Cube c, Cube.Side side, Cube neighbor, Cube.Side neighborSide)
		{
			updateCubes (c,neighbor);
		}
		
		void PrintCubeArray(){
			string sc = string.Empty;
			foreach(Cube c in cubes){
				sc += c == null ? "null;" : c.UniqueId;
			}
			Log.Debug (sc + " : " + numCubesArranged);
		}
		
		void AddCubeToArray(Cube c, int index){
			if (index < 0 || index >= cubes.Length)
				return;
			RemoveCubeFromArray (c);
			if (cubes[index] != null)
				RemoveCubeFromArray (cubes[index]);
			c.FillScreen(Color.Black);
			c.Image("heart_"+heartChar+"_"+index, 0, 0, 0, 0, 128, 128, 1, 0);
			c.Paint();
			cubes[index] = c;
		}
		
		/// <summary>
		/// Removes the Cube from the Array. Also clears Cube's screen.
		/// </summary>
		/// <returns>
		/// <c>true</c> iff the Cube was previously in the Array
		/// </returns>
		/// <param name='c'>
		/// The specific Cube object to remove from the Array
		/// </param>
		bool RemoveCubeFromArray(Cube c){
			int index = IsCubeInArray (c);
			if (index >= 0){
				cubes[index] = null;
				c.FillScreen(Color.White);
				c.Paint();
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Determines whether the Cube is in the Array
		/// </summary>
		/// <returns>
		/// the index of the Cube in the Array, 
		/// or -1 if the Cube is not in the Array
		/// </returns>
		/// <param name='c'>
		/// The specific Cube object to check if it is in the Array
		/// </param>
		int IsCubeInArray(Cube c){
			for(int i = 0; i < cubes.Length; i++){
				if (cubes[i] == c){
					return i;
				}
			}
			return -1;
		}
	}
}

