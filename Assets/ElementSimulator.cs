using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class ElementSimulator : MonoBehaviour {

	unsafe struct Bin{ // WARNING: variables must correspond to ElementSimulator.compute's Bin!
		public uint ID;
		public uint PosX;
		public uint PosY;
		public uint IsDirty;

		public uint Load;
		public fixed uint Contents[BIN_MAX_AMOUNT_OF_CONTENT];
		public uint ClusterLoad;
		public fixed uint ClusterContents[BIN_CLUSTER_CONTENT_MAX];
		
		public Vector4 Color;

		public static int GetStride() {
			return sizeof(uint) * 6 + sizeof(uint) * BIN_MAX_AMOUNT_OF_CONTENT + sizeof(uint) * BIN_CLUSTER_CONTENT_MAX + sizeof(float) * 4; // must correspond to variables!
		}
	};

	unsafe struct Particle{ // WARNING: variables must correspond to ElementSimulator.compute's Particle!
		public Vector2 Pos;
		public Vector2 Velocity;
		public Vector2 Force;
		public float Density;
		public float Pressure;
		public float Temperature;
		public float TemperatureStartFrame;
		public float RepelFactor;
		public float IsActive; // every thread needs a particle, so some will get inactive particles instead
		public Vector4 ParticlesToHeat;
		public Vector4 HeatToGive;

		public uint ElementIndex;
		public uint BinID;


		public static int GetStride() { // should preferably be multiple of 128
			return sizeof(float) * 20 + sizeof(uint) * 2; // must correspond to variables!
		}
	}

	private const int THREAD_COUNT_MAX = 1024;

	private const int START_PARTICLE_COUNT = 131072; // must be divisible by THREAD_COUNT_X!
	private const int START_PARTICLE_COUNT_ACTIVE = 131072;
	
	//#region[rgba(80, 0, 0, 1)] | WARNING: shared with ElementSimulator.compute! must be equal!

	private const int OUTPUT_THREAD_COUNT_X = 32;
	private const int OUTPUT_THREAD_COUNT_Y = 32;

	private const int BINS_THREAD_COUNT = 32;

	private const int THREAD_COUNT_X = 64;
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 64;
	private const int GRID_HEIGHT_TILES = 64;
	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
	private const int BIN_SIZE = 8;
	private const int BIN_COUNT_X = GRID_WIDTH_PIXELS / BIN_SIZE;
	private const int BIN_COUNT_Y = GRID_HEIGHT_PIXELS / BIN_SIZE;
	private const int BIN_MAX_AMOUNT_OF_CONTENT = 16;
	private const int BIN_CLUSTER_SIZE = 9;
	private const int BIN_CLUSTER_CONTENT_MAX = BIN_CLUSTER_SIZE * BIN_MAX_AMOUNT_OF_CONTENT;
	//#endregion

	// kernels
	private const string KERNEL_INIT = "Init";
	private const string KERNEL_INITBINS = "InitBins";
	private const string KERNEL_CLEAROUTPUTTEXTURE = "ClearOutputTexture";
	private const string KERNEL_CACHEPARTICLESINBINS = "CacheParticlesInBins";
	private const string KERNEL_CACHECLUSTERSINBINS = "CacheClustersInBins";
	private const string KERNEL_COMPUTEDENSITY = "ComputeDensity";
	private const string KERNEL_COMPUTEPRESSURE = "ComputePressure";
	private const string KERNEL_COMPUTEHEAT = "ComputeHeat";
	private const string KERNEL_APPLYHEAT = "ApplyHeat";
	private const string KERNEL_COMPUTEFORCES = "ComputeForces";
	private const string KERNEL_INTEGRATE = "Integrate";
	private const string KERNEL_PREPAREPOSTPROCESS = "PreparePostProcess";
	private const string KERNEL_POSTPROCESS = "PostProcess";
	private int kernelID_Init;
	private int kernelID_InitBins;
	private int kernelID_ClearOutputTexture;
	private int kernelID_CacheParticlesInBins;
	private int kernelID_CacheClustersInBins;
	private int kernelID_ComputeDensity;
	private int kernelID_ComputePressure;
	private int kernelID_ComputeHeat;
	private int kernelID_ApplyHeat;
	private int kernelID_ComputeForces;
	private int kernelID_Integrate;
	private int kernelID_PreparePostProcess;
	private int kernelID_PostProcess;

	// properties
	private const string PROPERTY_BINS = "bins";
	private const string PROPERTY_BINSATSTARTFRAME = "binsAtStartFrame";
	private const string PROPERTY_PARTICLES = "particles";
	private const string PROPERTY_PARTICLECOUNT = "particleCount";
	private const string PROPERTY_DEBUGVARS = "debugVars";
	private const string PROPERTY_OUTPUT = "output";
	private const string PROPERTY_ISFIRSTFRAME = "isFirstFrame";
	private const string PROPERTY_ISEVENFRAME = "isEvenFrame";
	private const string PROPERTY_DEBUGINDEX = "debugIndex";

	private int shaderPropertyID_bins;
	private int shaderPropertyID_binsAtStartFrame;
	private int shaderPropertyID_particles;
	private int shaderPropertyID_particleCount;
	private int shaderPropertyID_debugVars;
	private int shaderPropertyID_output;
	private int shaderPropertyID_isFirstFrame;
	private int shaderPropertyID_isEvenFrame;
	private int shaderPropertyID_debugIndex;

	private float updateInterval = 0.0f;//0.05f;
	private float nextTimeToUpdate = 0.0f;

	private float updateIntervalBins = 0.0f;// 0.075f;// 0.075f;// 0.075f;
	private float nextTimeToUpdateBins = 0.0f;

	private float updateIntervalHeat = 0.5f;
	private float nextTimeToUpdateHeat = 0.0f;

	private ComputeBuffer bufferBins;
	private ComputeBuffer bufferBinsAtStartFrame;
	private Bin[] bins;
	private Bin[] binsAtStartFrame;
	
	private ComputeBuffer bufferParticles;
	private Particle[] particles;

	private ComputeBuffer bufferDebug;

	private RenderTexture output;
	private Vector2[] uvs;

	[SerializeField]
	private ComputeShader shader;
	[SerializeField]
	private Material material;
	[SerializeField]
	private ParticleSystem particleSys;

	private bool isFirstFrame = true;
	private int frame = 0;


	void Awake(){
		kernelID_Init = shader.FindKernel(KERNEL_INIT);
		kernelID_InitBins = shader.FindKernel(KERNEL_INITBINS);
		kernelID_ClearOutputTexture = shader.FindKernel(KERNEL_CLEAROUTPUTTEXTURE);
		kernelID_CacheParticlesInBins = shader.FindKernel(KERNEL_CACHEPARTICLESINBINS);
		kernelID_CacheClustersInBins = shader.FindKernel(KERNEL_CACHECLUSTERSINBINS);
		kernelID_ComputeDensity = shader.FindKernel(KERNEL_COMPUTEDENSITY);
		kernelID_ComputePressure = shader.FindKernel(KERNEL_COMPUTEPRESSURE);
		kernelID_ComputeHeat = shader.FindKernel(KERNEL_COMPUTEHEAT);
		kernelID_ApplyHeat = shader.FindKernel(KERNEL_APPLYHEAT);
		kernelID_ComputeForces = shader.FindKernel(KERNEL_COMPUTEFORCES);
		kernelID_Integrate = shader.FindKernel(KERNEL_INTEGRATE);
		kernelID_PreparePostProcess = shader.FindKernel(KERNEL_PREPAREPOSTPROCESS);
		kernelID_PostProcess = shader.FindKernel(KERNEL_POSTPROCESS);

		shaderPropertyID_bins = Shader.PropertyToID(PROPERTY_BINS);
		shaderPropertyID_binsAtStartFrame = Shader.PropertyToID(PROPERTY_BINSATSTARTFRAME);
		shaderPropertyID_particles = Shader.PropertyToID(PROPERTY_PARTICLES);
		shaderPropertyID_particleCount = Shader.PropertyToID(PROPERTY_PARTICLECOUNT);
		shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
		shaderPropertyID_isFirstFrame = Shader.PropertyToID(PROPERTY_ISFIRSTFRAME);
		shaderPropertyID_isEvenFrame = Shader.PropertyToID(PROPERTY_ISEVENFRAME);
		shaderPropertyID_debugIndex = Shader.PropertyToID(PROPERTY_DEBUGINDEX);
	}

	void OnDisable(){
		bufferBins.Dispose();
		bufferBinsAtStartFrame.Dispose();
		bufferParticles.Dispose();
	}
	
	void Start () {
		InitShader();
	}

	void InitShader(){
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		bins = new Bin[BIN_COUNT_X * BIN_COUNT_Y];
		binsAtStartFrame = new Bin[BIN_COUNT_X * BIN_COUNT_Y];
		particles = new Particle[START_PARTICLE_COUNT];

		output = new RenderTexture(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS, 24);
		output.enableRandomWrite = true;
		output.filterMode = FilterMode.Point;
		output.Create();

		bool reverse = false;
		float x = 0, y = 0;
		for (int i = 0; i < particles.Length; i++){
			if (i > 0){
				if (!reverse && i >= particles.Length * 0.5f){
					reverse = true;
					y = GRID_HEIGHT_PIXELS * 1.0f;
					x = GRID_WIDTH_PIXELS - 1;

					// y = 0;
					// x = GRID_WIDTH_PIXELS - 1;
				}

				float spacing = 4.0f;
				if (reverse){
					y -= spacing;
					if (y < 0){
						y = GRID_HEIGHT_PIXELS - 1 - spacing * 0.5f;
						x -= spacing;
					}
				}
				else{
					y += spacing;
					if (y >= GRID_HEIGHT_PIXELS * 1.0f){
						y = spacing * 0.5f;
						x += spacing;
					}
				}
				// if (reverse){
				// 	y += spacing;
				// 	if (y >= GRID_HEIGHT_PIXELS * 1.0f){
				// 		y = spacing * 0.5f;
				// 		x -= spacing;
				// 	}
				// }
				// else{
				// 	y += spacing;
				// 	if (y >= GRID_HEIGHT_PIXELS * 0.66f){
				// 		y = spacing * 0.5f;
				// 		x += spacing;
				// 	}
				// }
				// if (reverse){
				// 	x -= spacing;
				// 	if (x < 0){
				// 		x = GRID_WIDTH_PIXELS - 1 - spacing * 0.5f;
				// 		y -= spacing;
				// 	}
				// }
				// else{
				// 	x += spacing;
				// 	if (x >= GRID_WIDTH_PIXELS){
				// 		x = spacing * 0.5f;
				// 		y += spacing;
				// 	}
				// }
			}

			Particle particle = particles[i];

			particle.Pos = new Vector2(x + Random.value * 1.0f, y);
			particle.Temperature = reverse ? 350 : 350;
			particle.TemperatureStartFrame = particle.Temperature;
			particle.ElementIndex = (uint)(reverse ? 0 : 1);
			particle.IsActive = Mathf.Clamp01(Mathf.Sign(START_PARTICLE_COUNT_ACTIVE - (i + 1)));

			particles[i] = particle;
		}

		bufferBins = new ComputeBuffer(bins.Length, Bin.GetStride());
		bufferBinsAtStartFrame = new ComputeBuffer(binsAtStartFrame.Length, Bin.GetStride());
		bufferParticles = new ComputeBuffer(particles.Length, Particle.GetStride());
	}

	void Update() {
		if (Time.time < nextTimeToUpdate) return;
		nextTimeToUpdate = Time.time + updateInterval;
		UpdateShader();
	}

	void UpdateShader() {
		int binsThreadGroupCount = Mathf.CeilToInt((BIN_COUNT_X * BIN_COUNT_Y) / BINS_THREAD_COUNT);
		int particlesThreadGroupCountX = Mathf.CeilToInt(particles.Length / THREAD_COUNT_X);
		int outputThreadGroupCountX = Mathf.CeilToInt(GRID_WIDTH_PIXELS / OUTPUT_THREAD_COUNT_X);
		int outputThreadGroupCountY = Mathf.CeilToInt(GRID_HEIGHT_PIXELS / OUTPUT_THREAD_COUNT_Y);

		shader.SetBool(shaderPropertyID_isFirstFrame, isFirstFrame);
		shader.SetBool(shaderPropertyID_isEvenFrame, frame % 2 == 0);

		if (isFirstFrame){
			// Init
			bufferParticles.SetData(particles);
			shader.SetBuffer(kernelID_Init, shaderPropertyID_particles, bufferParticles);
			shader.SetInt(shaderPropertyID_particleCount, START_PARTICLE_COUNT_ACTIVE);
			shader.Dispatch(kernelID_Init, particlesThreadGroupCountX, 1, 1);

			// InitBins
			bufferBins.SetData(bins);
			bufferBinsAtStartFrame.SetData(binsAtStartFrame);
			shader.SetBuffer(kernelID_InitBins, shaderPropertyID_bins, bufferBins);
			shader.Dispatch(kernelID_InitBins, binsThreadGroupCount, 1, 1);
		}

		// ClearOutputTexture
		if (isFirstFrame){
			shader.SetTexture(kernelID_ClearOutputTexture, shaderPropertyID_output, output);
		}
		Profiler.BeginSample("ClearOutputTexture");		
		shader.Dispatch(kernelID_ClearOutputTexture, outputThreadGroupCountX, outputThreadGroupCountY, 1);
		Profiler.EndSample();

		if (Time.time >= nextTimeToUpdateBins) { 
			nextTimeToUpdateBins = Time.time + updateIntervalBins;
		
			// CacheParticlesInBins
			if (isFirstFrame){
				shader.SetBuffer(kernelID_CacheParticlesInBins, shaderPropertyID_bins, bufferBins);
				shader.SetBuffer(kernelID_CacheParticlesInBins, shaderPropertyID_binsAtStartFrame, bufferBinsAtStartFrame);
				shader.SetBuffer(kernelID_CacheParticlesInBins, shaderPropertyID_particles, bufferParticles);
			}
			Profiler.BeginSample("CacheParticlesInBins");	
			shader.Dispatch(kernelID_CacheParticlesInBins, binsThreadGroupCount, 1, 1);
			Profiler.EndSample();

			// CacheParticleNeighbors
			if (isFirstFrame){
				shader.SetBuffer(kernelID_CacheClustersInBins, shaderPropertyID_bins, bufferBins);
			}
			Profiler.BeginSample("CacheClustersInBins");	
			shader.Dispatch(kernelID_CacheClustersInBins, binsThreadGroupCount, 1, 1);
			Profiler.EndSample();
		}

		// ComputeDensity
		if (isFirstFrame){
			shader.SetBuffer(kernelID_ComputeDensity, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ComputeDensity, shaderPropertyID_particles, bufferParticles);
		}
		Profiler.BeginSample("ComputeDensity");	
		shader.Dispatch	(kernelID_ComputeDensity, particlesThreadGroupCountX, 1, 1);
		Profiler.EndSample();

		// ComputePressure
		if (isFirstFrame){
			shader.SetBuffer(kernelID_ComputePressure, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ComputePressure, shaderPropertyID_particles, bufferParticles);
		}
		Profiler.BeginSample("ComputePressure");	
		shader.Dispatch(kernelID_ComputePressure, particlesThreadGroupCountX, 1, 1);
		Profiler.EndSample();

		if (Time.time >= nextTimeToUpdateHeat){
			nextTimeToUpdateHeat = Time.time + updateIntervalHeat;

			// ComputeHeat
			if (isFirstFrame){
			shader.SetBuffer(kernelID_ComputeHeat, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ComputeHeat, shaderPropertyID_particles, bufferParticles);
			}
			Profiler.BeginSample("ComputeHeat");	
			shader.Dispatch(kernelID_ComputeHeat, particlesThreadGroupCountX, 1, 1);
		Profiler.EndSample();

			// ApplyHeat
			if (isFirstFrame){
			shader.SetBuffer(kernelID_ApplyHeat, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ApplyHeat, shaderPropertyID_particles, bufferParticles);
			}
			Profiler.BeginSample("ApplyHeat");	
			shader.Dispatch(kernelID_ApplyHeat, particlesThreadGroupCountX, 1, 1);
			Profiler.EndSample();

		}

		// ComputeForces
		if (isFirstFrame){
			shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_particles, bufferParticles);
		}
		Profiler.BeginSample("ComputeForces");	
		shader.Dispatch(kernelID_ComputeForces, particlesThreadGroupCountX, 1, 1);
		Profiler.EndSample();

		// Integrate
		if (isFirstFrame){
			shader.SetBuffer(kernelID_Integrate, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_Integrate, shaderPropertyID_particles, bufferParticles);
			shader.SetTexture(kernelID_Integrate, shaderPropertyID_output, output);
		}
		Profiler.BeginSample("Integrate");	
		shader.Dispatch(kernelID_Integrate, particlesThreadGroupCountX, 1, 1);
		Profiler.EndSample();

		// PreparePostProcess
		if (isFirstFrame){
			shader.SetBuffer(kernelID_PreparePostProcess, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_PreparePostProcess, shaderPropertyID_particles, bufferParticles);
			shader.SetTexture(kernelID_PreparePostProcess, shaderPropertyID_output, output);
		}
		Profiler.BeginSample("PreparePostProcess");	
		shader.Dispatch(kernelID_PreparePostProcess, binsThreadGroupCount, 1, 1);
		Profiler.EndSample();

		// PostProcess
		if (isFirstFrame){
			shader.SetBuffer(kernelID_PostProcess, shaderPropertyID_bins, bufferBins);
			shader.SetTexture(kernelID_PostProcess, shaderPropertyID_output, output);
		}
		Profiler.BeginSample("PostProcess");	
		shader.Dispatch(kernelID_PostProcess, outputThreadGroupCountX, outputThreadGroupCountY, 1);
		Profiler.EndSample();

		material.mainTexture = output;

		frame++;
		isFirstFrame = false;
	}
}