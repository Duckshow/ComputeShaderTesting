using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leapfrog : MonoBehaviour {
    /*@T
    * \section{Leapfrog integration}
    * 
    * The leapfrog time integration scheme is frequently used in
    * particle simulation algorithms because
    * \begin{itemize}
    * \item It is explicit, which makes it easy to code.
    * \item It is second-order accurate.
    * \item It is {\em symplectic}, which means that it conserves
    *    certain properties of the continuous differential equation
    *    for Hamiltonian systems.  In practice, this means that it
    *    tends to conserve energy where energy is supposed to be
    *    conserved, assuming the time step is short enough for
    *    stability.
    * \end{itemize}
    * Of course, our system is {\em not} Hamiltonian -- viscosity
    * is a form of damping, so the system loses energy.  But we'll
    * stick with the leapfrog integration scheme anyhow.
    * 
    * The leapfrog time integration algorithm is named because
    * the velocities are updated on half steps and the positions
    * on integer steps; hence, the two leap over each other.
    * After computing accelerations, one step takes the form
    * \begin{align*}
    *   \bfv^{i+1/2} &= \bfv^{i-1/2} + \bfa^i \Delta t \\
    *   \bfr^{i+1}   &= \bfr^{i}     + \bfv^{i+1/2} \Delta t,
    * \end{align*}
    * This is straightforward enough, except for two minor points.
    * \begin{enumerate}
    * \item
    *   In order to compute the acceleration at time $t$, we need the
    *   velocity at time $t$.  But leapfrog only computes velocities at
    *   half steps!  So we cheat a little: when we compute the half-step
    *   velocity velocity $\bfv^{i+1/2}$ (stored in [[vh]]), we
    *   simultaneously compute an approximate integer step velocity
    *   $\tilde{\bfv}^{i+1}$ (stored in [[v]]) by taking another half
    *   step using the acceleration $\bfa^i$.
    * \item
    *   We don't explicitly represent the boundary by fixed particles,
    *   so we need some way to enforce the boundary conditions.  We take
    *   the simple approach of explicitly reflecting the particles using
    *   the [[reflect_bc]] routine discussed below.
    * \end{enumerate}
    *@c*/

    public static void leapfrog_step(State.sim_state_t s, float dt){
        for (int i = 0; i < s.n; ++i) {
            State.particle_t p = s.part[i];
            Utilities.Math_SAXPY(ref p.vh, dt,  p.a);
            p.v = p.vh;
            Utilities.Math_SAXPY(ref p.v, dt/2.0f, p.a);
            Utilities.Math_SAXPY(ref p.x, dt,   p.v);
        }
        reflect_bc(s);
    }

    /*@T
    * At the first step, the leapfrog iteration only has the initial
    * velocities $\bfv^0$, so we need to do something special.
    * \begin{align*}
    *   \bfv^{1/2} &= \bfv^0 + \bfa^0 \Delta t/2 \\
    *   \bfr^{1} &= \bfr^0 + \bfv^{1/2} \Delta t.
    * \end{align*}
    *@c*/

    public static void leapfrog_start(State.sim_state_t s, float dt){
        int n = s.n;
        for (int i = 0; i < s.n; i++) {
            State.particle_t p = s.part[i];
			p.vh = p.v;
			
            Utilities.Math_SAXPY(ref p.vh, dt/2, p.a);
            Utilities.Math_SAXPY(ref p.v,  dt,   p.a);
            Utilities.Math_SAXPY(ref p.x,  dt,   p.vh);
		}
        reflect_bc(s);
	}

    /*@T
    *
    * \section{Reflection boundary conditions}
    *
    * Our boundary condition corresponds to hitting an inelastic boundary
    * with a specified coefficient of restitution less than one.  When
    * a particle hits a barrier, we process it with [[damp_reflect]].
    * This reduces the total distance traveled based on the time since
    * the collision reflected, damps the velocities, and reflects
    * whatever solution components should be reflected.
    *@c*/

    static void damp_reflect(int which, float barrier, ref Vector3 x, ref Vector3 v, ref Vector3 vh){
        // Coefficient of resitiution
        const float DAMP = 0.75f;

		Vector3 xCached = x;
		Vector3 vCached = v;
		Vector3 vhCached = vh;

		// Ignore degenerate cases
		if (Mathf.Approximately(v.GetAxis(which), 0.0f))
            return;

        // Scale back the distance traveled based on time from collision
        float tbounce = (x.GetAxis(which)-barrier)/v.GetAxis(which);
        Utilities.Math_SAXPY(ref x, -(1-DAMP)*tbounce, v);

        // Reflect the position and velocity
        x.SetAxis(which, 2*barrier-x.GetAxis(which));
        v.SetAxis(which, -v.GetAxis(which));
        vh.SetAxis(which, -vh.GetAxis(which));

        // Damp the velocities
        v *= DAMP;
        vh *= DAMP;
		//Debug.Log("X = " + (2 * barrier - x.GetAxis(which)) + " (" + xCached.GetAxis(which) + ")");
		//Debug.LogFormat("x: {0}/{1}, \nv: {2}/{3}, \nvh: {4}/{5}\n", xCached * 1000, x * 1000, vCached * 1000, v * 1000, vhCached * 1000, vh * 1000);
	}

    /*@T
    *
    * For each particle, we need to check for reflections on each
    * of the four walls of the computational domain.
    *@c*/
    static void reflect_bc(State.sim_state_t s){
        // Boundaries of the computational domain
        const float XMIN = 0.0f;
        const float XMAX = 1.0f;
        const float YMIN = 0.0f;
        const float YMAX = 1.0f;
        const float ZMIN = 0.0f;
        const float ZMAX = 1.0f;

        for (int i = 0; i < s.n; i++) {
            State.particle_t part = s.part[i];

			if (part.x.x <= XMIN) damp_reflect(0, XMIN, ref part.x, ref part.v, ref part.vh);
            if (part.x.x >= XMAX) damp_reflect(0, XMAX, ref part.x, ref part.v, ref part.vh);
            if (part.x.y <= YMIN) damp_reflect(1, YMIN, ref part.x, ref part.v, ref part.vh);
            if (part.x.y >= YMAX) damp_reflect(1, YMAX, ref part.x, ref part.v, ref part.vh);
            if (part.x.z <= ZMIN) damp_reflect(2, ZMIN, ref part.x, ref part.v, ref part.vh);
            if (part.x.z >= ZMAX) damp_reflect(2, ZMAX, ref part.x, ref part.v, ref part.vh);

			part.x.x = Mathf.Clamp(part.x.x, XMIN, XMAX);
			part.x.y = Mathf.Clamp(part.x.y, YMIN, YMAX);
			part.x.z = Mathf.Clamp(part.x.z, ZMIN, ZMAX);
		}
    }
}
