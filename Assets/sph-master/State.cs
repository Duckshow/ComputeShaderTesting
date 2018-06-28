using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State {

	public const int STATE_HASH_SIZE = 4096;

	/*@T
	* \section{System state}
	* 
	* The [[sim_state_t]] structure holds the information for the current
	* state of the system and of the integration algorithm.
	* 
	* The [[alloc_state]] and [[free_state]] functions take care of storage
	* for the local simulation state.
	*@c*/
	public class particle_t {
		public float rho;         /* Particle density   */
		public Vector3 x = new Vector3();      /* Particle positions */
		public Vector3 v = new Vector3();      /* Particle velocities (full step) */
		public Vector3 vh = new Vector3();       /* Particle velocities (half step) */
		public Vector3 a = new Vector3();      /* Particle accelerations */
		public particle_t next;  /* List link for spatial hashing */
	}

	public class sim_state_t {
		public int n;                /* Number of particles    */
		public float mass;           /* Particle mass          */
		public particle_t[] part;     /* Particles              */
		public particle_t[] hash;    /* Hash table             */

		public sim_state_t(int n){
			this.n = n;
			part = new particle_t[n];
			for (int i = 0; i < part.Length; i++){
				part[i] = new particle_t();
			}
			hash = new particle_t[BinHash.HASH_SIZE];
			for (int i = 0; i < hash.Length; i++){
				hash[i] = new particle_t();
			}
		}
	}
}
