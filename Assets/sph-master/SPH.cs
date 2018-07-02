using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class SPH : MonoBehaviour {

	[SerializeField]
	private ParticleSystem particleSys;

	private ParticleSystem.Particle[] cachedParticles;

	/*@q
	* ====================================================================
	*/

	/*@T
	* \section{Initialization}
	*
	* We've hard coded the computational domain to a unit box, but we'd prefer
	* to do something more flexible for the initial distribution of fluid.
	* In particular, we define the initial geometry of the fluid in terms of an
	* {\em indicator function} that is one for points in the domain occupied
	* by fluid and zero elsewhere.  A [[domain_fun_t]] is a pointer to an
	* indicator for a domain, which is a function that takes two floats and
	* returns 0 or 1.  Two examples of indicator functions are a little box
	* of fluid in the corner of the domain and a circular drop.
	*@c*/
	System.Func<float, float, float, int> domain_fun_t;

	int box_indicator(float x, float y, float z){
		return (x < 0.5f) && (y < 0.75f) && (z < 0.5f) ? 1 : 0;
	}

	int circ_indicator(float x, float y, float z){
		float dx = (x-0.5f);
		float dy = (y-0.5f);
		float dz = (z-0.5f);
		float r2 = dx*dx + dy*dy + dz*dz;
		return (r2 < 0.25f*0.25f*0.25f) ? 1 : 0;
	}

	int points_indicator(float x, float y, float z) {
		return (x >= 0.5f ||  x <= 0.55f) && (y == 0.5f) && (z == 0.5f) ? 1 : 0;
	}
	/*@T
	*
	* The [[place_particles]] routine fills a region (indicated by the
	* [[indicatef]] argument) with fluid particles.  The fluid particles
	* are placed at points inside the domain that lie on a regular mesh
	* with cell sizes of $h/1.3$.  This is close enough to allow the
	* particles to overlap somewhat, but not too much.
	*@c*/
	State.sim_state_t place_particles(Params.sim_param_t param, Func<float, float, float, int> indicatef){
		float h  = param.h;
		float hh = h/1.3f;

		// Count mesh points that fall in indicated region.
		int count = 0;
		for (float x = 0; x < 1; x += hh) { 
			for (float y = 0; y < 1; y += hh) { 
				for (float z = 0; z < 1; z += hh) { 
					count += indicatef(x,y,z);
				}
			}
		}

		count = 100;

		// Populate the particle data structure
		State.sim_state_t s = new State.sim_state_t(count);
		int p = 0;
		for (float x = 0; x < 1; x += hh) {
			for (float y = 0; y < 1; y += hh) {
				for (float z = 0; z < 1; z += hh) {
					if (indicatef(x,y,z) == 1 && p < count) {
						s.part[p].x.Set(x, y, z);
						s.part[p].v.Set(0, 0, 0);
						p++;
					}
				}
			}
		}
		return s;    
	}

	/*@T
	*
	* The [[place_particle]] routine determines the initial particle
	* placement, but not the desired mass.  We want the fluid in the
	* initial configuration to exist roughly at the reference density.
	* One way to do this is to take the volume in the indicated body of
	* fluid, multiply by the mass density, and divide by the number of
	* particles; but that requires that we be able to compute the volume
	* of the fluid region.  Alternately, we can simply compute the
	* average mass density assuming each particle has mass one, then use
	* that to compute the particle mass necessary in order to achieve the
	* desired reference density.  We do this with [[normalize_mass]].
	* 
	* @c*/
	void normalize_mass(State.sim_state_t s, Params.sim_param_t param){
		s.mass = 1;
		BinHash.hash_particles(s, param.h);
		Interact.compute_density(s, param);
		float rho0 = param.rho0;
		float rho2s = 0;
		float rhos  = 0;
		for (int i = 0; i < s.n; i++) {
			rho2s += (s.part[i].rho)*(s.part[i].rho);
			rhos  += s.part[i].rho;
		}
		s.mass *= ( rho0*rhos / rho2s );
	}

	State.sim_state_t init_particles(Params.sim_param_t param){
		State.sim_state_t s = place_particles(param, box_indicator);
		//sim_state_t* s = place_particles(param, points_indicator);
		normalize_mass(s, param);
		return s;
	}

	/*@T
	* \section{The [[main]] event}
	*
	* The [[main]] routine actually runs the time step loop, writing
	* out files for visualization every few steps.  For debugging
	* convenience, we use [[check_state]] before writing out frames,
	* just so that we don't spend a lot of time on a simulation that
	* has gone berserk.
	*@c*/

	static bool check_state(State.sim_state_t s){
		for (int i = 0; i < s.n; i++) {
			float xi = s.part[i].x.GetAxis(0);
			float yi = s.part[i].x.GetAxis(1);
			float zi = s.part[i].x.GetAxis(2);
			if (xi < 0.0f || xi > 1.0f) { 
				Debug.LogError(xi + ": xi < 0 || xi > 1");
				return false;
			}
			if(yi < 0.0f || yi > 1.0f) {
				Debug.LogError(yi + ": yi < 0 || yi > 1");
				return false;
			}
			if(zi < 0.0f || zi > 1.0f) {
				Debug.LogError(zi + ": zi < 0 || zi > 1");
				return false;
			}
		}
		return true;
	}

	private bool isOkay = false;
	private State.sim_state_t state;
	private Params.sim_param_t param;
	private int framesRun = 0;

	void Start(){
		param = new Params.sim_param_t();
		Params.default_params(ref param);

		state = init_particles(param);

		particleSys.startSize = param.h;

		//write_header(fp, n);
		IO_TXT.write_header(state.n, param.nframes, param.h);
		IO_TXT.write_frame_data(state.n, state);
		Interact.compute_accel(state, param);
		Leapfrog.leapfrog_start(state, param.dt);
		isOkay = check_state(state);
// 		if (isOkay){
// 			for (int frame = 1; frame < param.nframes; frame++) {
// 				for (int i = 0; i < param.npframe; i++) {
// 					Interact.compute_accel(state, param);
// 					Leapfrog.leapfrog_step(state, param.dt);
// 					isOkay = check_state(state);
// 					if(!isOkay) break;

// 					UpdateParticleSystemParticleCount(state);
// 					ApplyToParticleSystem(state);
// 				}

// //				Debug.LogFormat("Frame: {0} of {1} - {2}%\n", frame, param.nframes, 100*(float)frame/param.nframes);
// 				IO_TXT.write_frame_data(state.n, state);
// 				if(!isOkay) break;
// 			}
// 		}

		//particleSys.Pause();
		Debug.LogFormat("Ran in {0} seconds\n", Time.time-Time.timeSinceLevelLoad);
	}

	void FixedUpdate() {
		for (int i = 0; i < param.npframe; i++){
			Interact.compute_accel(state, param);
			Leapfrog.leapfrog_step(state, param.dt);
			isOkay = check_state(state);
			if (!isOkay) break;

			UpdateParticleSystemParticleCount(state);
			ApplyToParticleSystem(state);
		}

//		Debug.LogFormat("Frame: {0} of {1} - {2}%\n", framesRun, param.nframes, 100 * (float)framesRun / param.nframes);
		IO_TXT.write_frame_data(state.n, state);
		framesRun++;
	}

	void UpdateParticleSystemParticleCount(State.sim_state_t state) {
		int maxAmount = state.part.Length;
		ParticleSystem.MainModule main = particleSys.main;
		main.maxParticles = maxAmount;

		if (cachedParticles == null || cachedParticles.Length != maxAmount){
			cachedParticles = new ParticleSystem.Particle[maxAmount];
		}

		if (particleSys.particleCount < maxAmount){
			particleSys.Play();
			particleSys.Emit(maxAmount - particleSys.particleCount);
		}
	}

	void ApplyToParticleSystem(State.sim_state_t state) {
		int currentParticleCount = particleSys.GetParticles(cachedParticles);

		//Debug.Log(currentParticleCount);
		for (int i = 0; i < state.part.Length; i++){
			cachedParticles[i].position = state.part[i].x;
			cachedParticles[i].velocity = state.part[i].v;
		}
		particleSys.SetParticles(cachedParticles, cachedParticles.Length);
	}
}
