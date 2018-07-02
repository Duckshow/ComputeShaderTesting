using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Params : MonoBehaviour {

	/*@T
	* \section{System parameters}
	* 
	* The [[sim_param_t]] structure holds the parameters that
	* describe the simulation.  These parameters are filled in
	* by the [[get_params]] function (described later).
	*@c*/
	public class sim_param_t {
		public string fname;   /* File name          */
		public int   nframes; /* Number of frames   */
		public int   npframe; /* Steps per frame    */
		public float h;       /* Particle size      */
		public float dt;      /* Time step          */
		public float rho0;    /* Reference density  */
		public float k;       /* Bulk modulus       */
		public float mu;      /* Viscosity          */
		public float g;       /* Gravity strength   */
	}

	/*@T
	* \section{Option processing}
	* 
	* The [[print_usage]] command documents the options to the [[nbody]]
	* driver program, and [[default_params]] sets the default parameter
	* values.  You may want to add your own options to control
	* other aspects of the program.  This is about as many options as
	* I would care to handle at the command line --- maybe more!  Usually,
	* I would start using a second language for configuration (e.g. Lua)
	* to handle anything more than this.
	*@c*/
	public static void default_params(ref sim_param_t param){
		param.fname   = "run_test.out";
		param.nframes = 100;
		param.npframe = 10;
		param.dt      = 1e-4f;
		param.h       = 5e-2f;
		param.rho0    = 1000f;
		param.k       = 1e3f;
		param.mu      = 0.1f;
		param.g       = 9.8f;
	}

	public static void print_usage(){
		sim_param_t param = new sim_param_t();
		default_params(ref param);
		Debug.LogFormat(
			"nbody\n"
			+ "\t-h: print this message\n"
			+ "\t-o: output file name (%s)\n"
			+ "\t-F: number of frames (%d)\n"
			+ "\t-f: steps per frame (%d)\n"
			+ "\t-t: time step (%e)\n"
			+ "\t-s: particle size (%e)\n"
			+ "\t-d: reference density (%g)\n"
			+ "\t-k: bulk modulus (%g)\n"
			+ "\t-v: dynamic viscosity (%g)\n"
			+ "\t-g: gravitational strength (%g)\n",
			param.fname, param.nframes, param.npframe,
			param.dt, param.h, param.rho0,
			param.k, param.mu, param.g
		);
	}

	/*@T
	*
	* The [[get_params]] function uses the [[getopt]] package
	* to handle the actual argument processing.  Note that
	* [[getopt]] is {\em not} thread-safe!  You will need to
	* do some synchronization if you want to use this function
	* safely with threaded code.
	*@c*/
	// int get_params(int argc, ref char argv, ref sim_param_t param){
	// 	default_params(ref param);
	// 	while ((c = getopt(argc, argv, optstring)) != -1) {
	// 		switch (c) {
	// 			case 'h': 
	// 				print_usage(); 
	// 				return -1;
	// 			case 'o':
	// 				strcpy(params->fname = malloc(strlen(optarg)+1), optarg);
	// 				break;
	// 			get_int_arg('F', nframes);
	// 			get_int_arg('f', npframe);
	// 			get_flt_arg('t', dt);
	// 			get_flt_arg('s', h);
	// 			get_flt_arg('d', rho0);
	// 			get_flt_arg('k', k);
	// 			get_flt_arg('v', mu);
	// 			get_flt_arg('g', g);
	// 			default:
	// 				fprintf(stderr, "Unknown option\n");
	// 				return -1;
	// 		}
	// 	}
	// 	return 0;
	// }
}
