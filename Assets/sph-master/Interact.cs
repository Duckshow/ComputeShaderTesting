using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour {
	/*@T
	* \subsection{Density computations}
	* 
	* The formula for density is
	* \[
	*   \rho_i = \sum_j m_j W_{p6}(r_i-r_j,h)
	*          = \frac{315 m}{64 \pi h^9} \sum_{j \in N_i} (h^2 - r^2)^3.
	* \]
	* We search for neighbors of node $i$ by checking every particle,
	* which is not very efficient.  We do at least take advange of
	* the symmetry of the update ($i$ contributes to $j$ in the same
	* way that $j$ contributes to $i$).
	*@c*/

	static void update_density(State.particle_t pi, State.particle_t pj, float h2, float C){
		float r2 = (pi.x - pj.x).sqrMagnitude;
		float z  = h2-r2;
		if (z > 0) {
			float rho_ij = C*z*z*z;
			pi.rho += rho_ij;
			pj.rho += rho_ij;
		}
	}

	public static void compute_density(State.sim_state_t s, Params.sim_param_t param){
		int n = s.n;
		//particle_t* p = s->part;
		//particle_t** hash = s->hash;

		float h  = param.h;
		float h2 = h*h;
		float h3 = h2*h;
		float h9 = h3*h3*h3;
		float C  = ( 315.0f/64.0f/Mathf.PI ) * s.mass / h9;

		// Clear densities
		for (int i = 0; i < n; ++i)
			s.part[i].rho = 0;

		// Accumulate density info
		// Create small stack array of size what we want
		uint[] neighborBucket = new uint[27];

		for (int i = 0; i < n; ++i) {
			State.particle_t pi = s.part[i];
			pi.rho += 4 * s.mass / Mathf.PI / h3;

			// Retrieve neighbors
			BinHash.particle_neighborhood(ref neighborBucket, pi, h);

			// Loop through neighbors
			for (int j = 0; j < 27; j++) {
				State.particle_t pj = s.hash[neighborBucket[j]];
				//printf("Point: %p\n", pj);
				if (pj != null) { // Go through linked list
					do {
						if (pi != pj) {
							update_density(pi, pj, h2, C);
						}
						pj = pj.next;
					} while (pj != null);
				}
			}
		}
	}


	/*@T
	* \subsection{Computing forces}
	* 
	* The acceleration is computed by the rule
	* \[
	*   \bfa_i = \frac{1}{\rho_i} \sum_{j \in N_i} 
	*     \bff_{ij}^{\mathrm{interact}} + \bfg,
	* \]
	* where the pair interaction formula is as previously described.
	* Like [[compute_density]], the [[compute_accel]] routine takes
	* advantage of the symmetry of the interaction forces
	* ($\bff_{ij}^{\mathrm{interact}} = -\bff_{ji}^{\mathrm{interact}}$)
	* but it does a very expensive brute force search for neighbors.
	*@c*/

	static void update_forces(State.particle_t pi, State.particle_t pj, float h2, float rho0, float C0, float Cp, float Cv){
		Vector3 dx = new Vector3();
		dx = pi.x - pj.x;
		float r2 = dx.sqrMagnitude;
		if (r2 < h2) {
			float rhoi = pi.rho;
			float rhoj = pj.rho;
			float q = Mathf.Sqrt(r2/h2);
			float u = 1-q;
			float w0 = C0 * u/rhoi/rhoj;
			float wp = w0 * Cp * (rhoi+rhoj-2*rho0) * u/q;
			float wv = w0 * Cv;
			Vector3 dv = pi.v - pj.v;
			
			// Equal and opposite pressure forces
			Utilities.Math_SAXPY(ref pi.a,  wp, dx);
			Utilities.Math_SAXPY(ref pj.a, -wp, dx);
			
			// Equal and opposite viscosity forces
			Utilities.Math_SAXPY(ref pi.a,  wv, dv);
			Utilities.Math_SAXPY(ref pj.a, -wv, dv);
		}
	}

	public static void compute_accel(State.sim_state_t state, Params.sim_param_t param){
		// Unpack basic parameters
		float h    = param.h;
		float rho0 = param.rho0;
		float k    = param.k;
		float mu   = param.mu;
		float g    = param.g;
		float mass = state.mass;
		float h2   = h*h;

		// Unpack system state
		//particle_t* p = state->part;
		//particle_t** hash = state->hash;
		int n = state.n;


		// Rehash the particles
		BinHash.hash_particles(state, h);

		// Compute density and color
		compute_density(state, param);

		// Start with gravity and surface forces
		for (int i = 0; i < n; ++i){
			state.part[i].a.Set(0, -g, 0);
		}

		// Constants for interaction term
		float C0 = 45 * mass / Mathf.PI / ( (h2)*(h2)*h );
		float Cp = k/2;
		float Cv = -mu;

		// Accumulate forces
		// Create small stack array of size what we want
		uint[] neighborBucket = new uint[27];

		for (int i = 0; i < n; i++) {
			State.particle_t pi = state.part[i];

			// Retrieve neighbors
			BinHash.particle_neighborhood(ref neighborBucket, pi, h);

			// Loop through neighbors
			for (int j = 0; j < 27; j++) {
				State.particle_t pj = state.hash[neighborBucket[j]];
				if (pj != null) { // Go through linked list
					do {
						if (pi != pj) { // Don't want to do crazy 
							update_forces(pi, pj, h2, rho0, C0, Cp, Cv);
						}
						pj = pj.next;
					} while (pj != null);
				}
			}
		}
	}
}
