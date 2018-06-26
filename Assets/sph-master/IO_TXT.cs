using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IO_TXT : MonoBehaviour {
	public static void write_header(int n, int framecount, float h){
		Debug.LogFormat("{0}, {1}, {2}\n", n, framecount, h);
	}

	public static void write_frame_data(int n, State.sim_state_t s){
		for (int i = 0; i < n; i++){
			Debug.LogFormat("{0}\n", s.part[i].x);
		}
	}
}
