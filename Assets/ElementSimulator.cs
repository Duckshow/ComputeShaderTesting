using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementSimulator : MonoBehaviour {

	struct Bin{ // WARNING: variables must correspond to ElementSimulator.compute's Bin!
		public uint ID;
		public uint PosX;
		public uint PosY;
		public uint IsDirty;
		public uint Load;
		public uint[] Contents;

		public static int GetStride() {
			return sizeof(uint) * 5 + sizeof(uint) * BIN_MAX_AMOUNT_OF_CONTENT; // must correspond to variables!
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
		public float DebugTemp;
		// public float DebugThermal;
		public float Debug1;
		public float Debug2;
		public float Debug3;
		public float Debug4;
		public float Debug5;
		public float Debug6;
		public float Debug7;
		public float Debug8;
		public float Debug9;
		public float Debug10;
		public float Debug11;
		public float Debug12;
		public float Debug13;

		// 92 byte
		// public float padding_0;
		// public float padding_1;
		// public float padding_2;
		// public float padding_3;
		// public float padding_4;
		// public float padding_5;
		// public float padding_6;
		// public float padding_7;
		// public float padding_8;
		// 128 byte

		public uint ElementIndex;

		public uint BinPosX;
		public uint BinPosY;
		public uint ClusterLoad;
		public fixed uint ClusterContents[BIN_CLUSTER_CONTENT_MAX]; // size of BIN_CLUSTER_CONTENT_MAX

		public static int GetStride() {
			return sizeof(float) * 34/*35*/ + sizeof(uint) * 4 + sizeof(uint) * BIN_CLUSTER_CONTENT_MAX; // must correspond to variables!
		}
	}

	// struct DebugVars{ // WARNING: variables must correspond to ElementSimulator.compute's Particle!
	// 	public float HasNewValue;
	// 	public Vector2 DebugID;
	// 	public float Debug_00;
	// 	public float Debug_01;
	// 	public float Debug_02;
	// 	public float Debug_03;
	// 	public float Debug_04;
	// 	public float Debug_05;
	// 	public float Debug_06;
	// 	public float Debug_07;
	// 	public float Debug_08;
	// 	public float Debug_09;
	// 	public float Debug_10;
	// 	public float Debug_11;
	// 	public float Debug_12;
	// 	public float Debug_13;
	// 	public float Debug_14;
	// 	public float Debug_15;
	// 	public float Debug_16;
	// 	public float Debug_17;
	// 	public float Debug_18;
	// 	public float Debug_19;
	// 	public float Debug_20;
	// 	public float Debug_21;
	// 	public float Debug_22;
	// 	public float Debug_23;
	// 	public float Debug_24;
	// 	public float Debug_25;
	// 	public float Debug_26;
	// 	public float Debug_27;
	// 	public float Debug_28;
	// 	public float Debug_29;
	// 	public float Debug_30;
	// 	public float Debug_31;
	// 	public float Debug_32;
	// 	public float Debug_33;
	// 	public float Debug_34;
	// 	public float Debug_35;
	// 	public float Debug_36;
	// 	public float Debug_37;
	// 	public float Debug_38;
	// 	public float Debug_39;
	// 	public float Debug_40;
	// 	public float Debug_41;
	// 	public float Debug_42;
	// 	public float Debug_43;
	// 	public float Debug_44;
	// 	public float Debug_45;
	// 	public float Debug_46;
	// 	public float Debug_47;
	// 	public float Debug_48;
	// 	public float Debug_49;

	// 	public static int GetStride() {
	// 		return sizeof(float) * 53; // must correspond to variables!
	// 	}

	// 	public void Print() {
	// 		if(HasNewValue == 0) return;
	// 		HasNewValue = 0;

	// 		Debug.Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
	// 		Debug.Log("ID: " + DebugID);
	// 		Debug.Log("Debug_00: " + Debug_00);
	// 		Debug.Log("Debug_01: " + Debug_01);
	// 		Debug.Log("Debug_02: " + Debug_02);
	// 		Debug.Log("Debug_03: " + Debug_03);
	// 		Debug.Log("Debug_04: " + Debug_04);
	// 		Debug.Log("Debug_05: " + Debug_05);
	// 		Debug.Log("Debug_06: " + Debug_06);
	// 		Debug.Log("Debug_07: " + Debug_07);
	// 		Debug.Log("Debug_08: " + Debug_08);
	// 		Debug.Log("Debug_09: " + Debug_09);
	// 		Debug.Log("Debug_10: " + Debug_10);
	// 		Debug.Log("Debug_11: " + Debug_11);
	// 		Debug.Log("Debug_12: " + Debug_12);
	// 		Debug.Log("Debug_13: " + Debug_13);
	// 		Debug.Log("Debug_14: " + Debug_14);
	// 		Debug.Log("Debug_15: " + Debug_15);
	// 		Debug.Log("Debug_16: " + Debug_16);
	// 		Debug.Log("Debug_17: " + Debug_17);
	// 		Debug.Log("Debug_18: " + Debug_18);
	// 		Debug.Log("Debug_19: " + Debug_19);
	// 		Debug.Log("Debug_20: " + Debug_20);
	// 		Debug.Log("Debug_21: " + Debug_21);
	// 		Debug.Log("Debug_22: " + Debug_22);
	// 		Debug.Log("Debug_23: " + Debug_23);
	// 		Debug.Log("Debug_24: " + Debug_24);
	// 		Debug.Log("Debug_25: " + Debug_25);
	// 		Debug.Log("Debug_26: " + Debug_26);
	// 		Debug.Log("Debug_27: " + Debug_27);
	// 		Debug.Log("Debug_28: " + Debug_28);
	// 		Debug.Log("Debug_29: " + Debug_29);
	// 		Debug.Log("Debug_30: " + Debug_30);
	// 		Debug.Log("Debug_30: " + Debug_30);
	// 		Debug.Log("Debug_31: " + Debug_31);
	// 		Debug.Log("Debug_32: " + Debug_32);
	// 		Debug.Log("Debug_33: " + Debug_33);
	// 		Debug.Log("Debug_34: " + Debug_34);
	// 		Debug.Log("Debug_35: " + Debug_35);
	// 		Debug.Log("Debug_36: " + Debug_36);
	// 		Debug.Log("Debug_37: " + Debug_37);
	// 		Debug.Log("Debug_38: " + Debug_38);
	// 		Debug.Log("Debug_39: " + Debug_39);
	// 		Debug.Log("Debug_40: " + Debug_40);
	// 		Debug.Log("Debug_40: " + Debug_40);
	// 		Debug.Log("Debug_41: " + Debug_41);
	// 		Debug.Log("Debug_42: " + Debug_42);
	// 		Debug.Log("Debug_43: " + Debug_43);
	// 		Debug.Log("Debug_44: " + Debug_44);
	// 		Debug.Log("Debug_45: " + Debug_45);
	// 		Debug.Log("Debug_46: " + Debug_46);
	// 		Debug.Log("Debug_47: " + Debug_47);
	// 		Debug.Log("Debug_48: " + Debug_48);
	// 		Debug.Log("Debug_49: " + Debug_49);
	// 		Debug.Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
	// 	}
	// }

	private const int THREAD_COUNT_MAX = 1024;

	private const int START_PARTICLE_COUNT = 4096; // must be divisible by THREAD_COUNT_X!
	private const int START_PARTICLE_COUNT_ACTIVE = 4096;
	
	//#region[rgba(80, 0, 0, 1)] | WARNING: shared with ElementSimulator.compute! must be equal!

	private const int OUTPUT_THREAD_COUNT_X = 32;
	private const int OUTPUT_THREAD_COUNT_Y = 32;

	private const int BINS_THREAD_COUNT = 64;

	private const int THREAD_COUNT_X = 64;
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 32;
	private const int GRID_HEIGHT_TILES = 32;
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
	private const string KERNEL_CACHEPARTICLENEIGHBORS = "CacheParticleNeighbors";
	private const string KERNEL_COMPUTEDENSITY = "ComputeDensity";
	private const string KERNEL_COMPUTEPRESSURE = "ComputePressure";
	private const string KERNEL_COMPUTEHEAT = "ComputeHeat";
	private const string KERNEL_APPLYHEAT = "ApplyHeat";
	private const string KERNEL_COMPUTEFORCES = "ComputeForces";
	private const string KERNEL_INTEGRATE = "Integrate";
	private int kernelID_Init;
	private int kernelID_InitBins;
	private int kernelID_ClearOutputTexture;
	private int kernelID_CacheParticlesInBins;
	private int kernelID_CacheParticleNeighbors;
	private int kernelID_ComputeDensity;
	private int kernelID_ComputePressure;
	private int kernelID_ComputeHeat;
	private int kernelID_ApplyHeat;
	private int kernelID_ComputeForces;
	private int kernelID_Integrate;

	// properties
	private const string PROPERTY_BINS = "bins";
	private const string PROPERTY_PARTICLES = "particles";
	private const string PROPERTY_PARTICLECOUNT = "particleCount";
	// private const string PROPERTY_DEBUGBININDEX_X = "debugBinIndexX";
	// private const string PROPERTY_DEBUGBININDEX_Y = "debugBinIndexY";
	private const string PROPERTY_DEBUGVARS = "debugVars";
	private const string PROPERTY_OUTPUT = "output";
	private const string PROPERTY_ISFIRSTFRAME = "isFirstFrame";
	private const string PROPERTY_ISEVENFRAME = "isEvenFrame";
	private const string PROPERTY_DEBUGINDEX = "debugIndex";

	private int shaderPropertyID_bins;
	private int shaderPropertyID_particles;
	private int shaderPropertyID_particleCount;
	// private int shaderPropertyID_debugBinIndexX;
	// private int shaderPropertyID_debugBinIndexY;
	private int shaderPropertyID_debugVars;
	private int shaderPropertyID_output;
	private int shaderPropertyID_isFirstFrame;
	private int shaderPropertyID_isEvenFrame;
	private int shaderPropertyID_debugIndex;

	private float updateInterval = 0.0f;
	private float nextTimeToUpdate = 0.0f;

	private float updateIntervalBins = 0.0f;// 0.075f;// 0.075f;
	private float nextTimeToUpdateBins = 0.0f;

	private float updateIntervalHeat = 0.5f;
	private float nextTimeToUpdateHeat = 0.0f;

	private ComputeBuffer bufferBins;
	private Bin[] bins;
	
	private ComputeBuffer bufferParticles;
	private Particle[] particles;

	private ComputeBuffer bufferDebug;
	// private DebugVars[] debugVars;

	private RenderTexture output;
	private Vector2[] uvs;

	[SerializeField]
	private ComputeShader shader;
	[SerializeField]
	private Material material;
	[SerializeField]
	private ParticleSystem particleSys;

	// [SerializeField]
	// private Vector2 debugBinIndex;

	// private int debugIndex = -1;

	private bool isFirstFrame = true;
	private int frame = 0;

	[Space]
	[SerializeField]
	private int particleIndex;
	[SerializeField]
	private bool printOnStart = false;
	[EasyButtons.Button]
	public void PrintParticle() {
		if(particleIndex >= particles.Length) return;

		Debug.Log("=====================================");
		Debug.Log(particleIndex + ", (" + particles[particleIndex].Pos + "), (" + Mathf.Floor(particles[particleIndex].Pos.x / (float)BIN_SIZE) + ", " + Mathf.Floor(particles[particleIndex].Pos.y / (float)BIN_SIZE) + "):");
		// Debug.Log("IsActive = " + particles[particleIndex].IsActive);
		//Debug.Log("Temperature = " + particles[particleIndex].DebugTemp);
		//Debug.Log("ThermalDiff = " + particles[particleIndex].DebugThermal);
		Debug.Log("Debug1 = " + particles[particleIndex].Debug1);
		Debug.Log("Debug2 = " + particles[particleIndex].Debug2);
		Debug.Log("Debug3 = " + particles[particleIndex].Debug3);
		Debug.Log("Debug4 = " + particles[particleIndex].Debug4);
		Debug.Log("Debug5 = " + particles[particleIndex].Debug5);
		Debug.Log("Debug6 = " + particles[particleIndex].Debug6);
		Debug.Log("Debug7 = " + particles[particleIndex].Debug7);
		Debug.Log("Debug8 = " + particles[particleIndex].Debug8);
		Debug.Log("Debug9 = " + particles[particleIndex].Debug9);
		Debug.Log("Debug10 = " + particles[particleIndex].Debug10);
		Debug.Log("Debug11 = " + particles[particleIndex].Debug11);
		Debug.Log("Debug12 = " + particles[particleIndex].Debug12);
		Debug.Log("Debug13 = " + particles[particleIndex].Debug13);
		Debug.Log("=====================================");
	}

	void OnValidate() { 
		// if (particles == null || particles.Length == 0) return;

		// ParticleSystem.Particle[] unityParticles = new ParticleSystem.Particle[particles.Length];
		// int particleCount = particleSys.GetParticles(unityParticles);
		// for (int i = 0; i < unityParticles.Length; i++){
		// 	Particle particle = particles[i];
		// 	ParticleSystem.Particle unityParticle = unityParticles[i];

		// 	Color color = Color.Lerp(Color.blue, Color.red, particle.Temperature / 1000.0f);
		// 	if (particleIndex == i){
		// 		color = Color.cyan;
		// 	}
		// 	// else if (particle.DebugTemp > 0){
		// 	// 	color = Color.green;
		// 	// }

		// 	color.a = particle.IsActive;
		// 	unityParticle.startColor = color;

		// 	unityParticles[i] = unityParticle;
		// }
		// particleSys.SetParticles(unityParticles, particleCount);
	}

	void Awake(){
		kernelID_Init = shader.FindKernel(KERNEL_INIT);
		kernelID_InitBins = shader.FindKernel(KERNEL_INITBINS);
		kernelID_ClearOutputTexture = shader.FindKernel(KERNEL_CLEAROUTPUTTEXTURE);
		kernelID_CacheParticlesInBins = shader.FindKernel(KERNEL_CACHEPARTICLESINBINS);
		kernelID_CacheParticleNeighbors = shader.FindKernel(KERNEL_CACHEPARTICLENEIGHBORS);
		kernelID_ComputeDensity = shader.FindKernel(KERNEL_COMPUTEDENSITY);
		kernelID_ComputePressure = shader.FindKernel(KERNEL_COMPUTEPRESSURE);
		kernelID_ComputeHeat = shader.FindKernel(KERNEL_COMPUTEHEAT);
		kernelID_ApplyHeat = shader.FindKernel(KERNEL_APPLYHEAT);
		kernelID_ComputeForces = shader.FindKernel(KERNEL_COMPUTEFORCES);
		kernelID_Integrate = shader.FindKernel(KERNEL_INTEGRATE);

		shaderPropertyID_bins = Shader.PropertyToID(PROPERTY_BINS);
		shaderPropertyID_particles = Shader.PropertyToID(PROPERTY_PARTICLES);
		shaderPropertyID_particleCount = Shader.PropertyToID(PROPERTY_PARTICLECOUNT);
		// shaderPropertyID_debugBinIndexX = Shader.PropertyToID(PROPERTY_DEBUGBININDEX_X);
		// shaderPropertyID_debugBinIndexY = Shader.PropertyToID(PROPERTY_DEBUGBININDEX_Y);
		// shaderPropertyID_debugVars = Shader.PropertyToID(PROPERTY_DEBUGVARS);
		shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
		shaderPropertyID_isFirstFrame = Shader.PropertyToID(PROPERTY_ISFIRSTFRAME);
		shaderPropertyID_isEvenFrame = Shader.PropertyToID(PROPERTY_ISEVENFRAME);
		shaderPropertyID_debugIndex = Shader.PropertyToID(PROPERTY_DEBUGINDEX);
	}

	void OnDisable(){
		bufferBins.Dispose();
		bufferParticles.Dispose();
		// bufferDebug.Dispose();
	}
	
	void Start () {
		InitShader();
		particleSys.Play();
		particleSys.Emit(particles.Length);
	}

	void InitShader(){
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		bins = new Bin[BIN_COUNT_X * BIN_COUNT_Y];
		particles = new Particle[START_PARTICLE_COUNT];
		// debugVars = new DebugVars[1];

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
					y = GRID_HEIGHT_PIXELS;
					x = GRID_WIDTH_PIXELS - 1;
				}

				float spacing = 8.0f;
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
			// particle.Temperature = Random.Range(0, 1000);
			// particle.Temperature = x < GRID_WIDTH_PIXELS * 0.5f ? 10000 : 0;
			particle.Temperature = reverse ? 0 : 300;
			particle.TemperatureStartFrame = particle.Temperature;
			particle.ElementIndex = 0;
			particle.IsActive = Mathf.Clamp01(Mathf.Sign(START_PARTICLE_COUNT_ACTIVE - (i + 1)));

			particles[i] = particle;
		}
		// int hotCount = 0;
		// for (int i = 0; i < particles.Length; i++){
		// 	if(hotCount >= 20) break;
		// 	if(Random.value > 0.01) continue;
		// 	hotCount++;
		// 	particles[i].Temperature = 10000;
		// 	particles[i].TemperatureStartFrame = 10000;
		// }
		bufferBins = new ComputeBuffer(bins.Length, Bin.GetStride());
		bufferParticles = new ComputeBuffer(particles.Length, Particle.GetStride());
		// bufferDebug = new ComputeBuffer(1, DebugVars.GetStride());
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
		shader.SetFloat(shaderPropertyID_debugIndex, particleIndex);

		if (isFirstFrame){
			// Init
			bufferParticles.SetData(particles);
			shader.SetBuffer(kernelID_Init, shaderPropertyID_particles, bufferParticles);
			shader.SetInt(shaderPropertyID_particleCount, START_PARTICLE_COUNT_ACTIVE);
			shader.Dispatch(kernelID_Init, Mathf.CeilToInt(particles.Length / THREAD_COUNT_X), 1, 1);
			// bufferParticles.GetData(particles);

			// InitBins
			bufferBins.SetData(bins);
			shader.SetBuffer(kernelID_InitBins, shaderPropertyID_bins, bufferBins);
			shader.Dispatch(kernelID_InitBins, Mathf.CeilToInt(bins.Length / BINS_THREAD_COUNT), 1, 1);
			// bufferBins.GetData(bins);
		}

		// ClearOutputTexture
		if (isFirstFrame){
			shader.SetTexture(kernelID_ClearOutputTexture, shaderPropertyID_output, output);
		}
		shader.Dispatch(kernelID_ClearOutputTexture, outputThreadGroupCountX, outputThreadGroupCountY, 1);

		if (Time.time >= nextTimeToUpdateBins) { 
			nextTimeToUpdateBins = Time.time + updateIntervalBins;

			// CacheParticlesInBins
			if (isFirstFrame){
				// bufferBins.SetData(bins);
				// bufferParticles.SetData(particles);
				shader.SetBuffer(kernelID_CacheParticlesInBins, shaderPropertyID_bins, bufferBins);
				shader.SetBuffer(kernelID_CacheParticlesInBins, shaderPropertyID_particles, bufferParticles);
			}
			shader.Dispatch(kernelID_CacheParticlesInBins, binsThreadGroupCount, 1, 1);
		}

		// CacheParticleNeighbors
		if (isFirstFrame){
			shader.SetBuffer(kernelID_CacheParticleNeighbors, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_CacheParticleNeighbors, shaderPropertyID_particles, bufferParticles);
		}
		// bufferDebug.SetData(debugVars);
		// shader.SetBuffer(kernelID_CacheParticleNeighbors, shaderPropertyID_debugVars, bufferDebug);
		shader.Dispatch(kernelID_CacheParticleNeighbors, particlesThreadGroupCountX, 1, 1);
		// bufferDebug.GetData(debugVars);

		// ComputeDensity
		if (isFirstFrame){
			shader.SetBuffer(kernelID_ComputeDensity, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ComputeDensity, shaderPropertyID_particles, bufferParticles);
		}
		shader.Dispatch	(kernelID_ComputeDensity, particlesThreadGroupCountX, 1, 1);

		// // ComputePressure
		if (isFirstFrame){
			shader.SetBuffer(kernelID_ComputePressure, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ComputePressure, shaderPropertyID_particles, bufferParticles);
		}
		shader.Dispatch(kernelID_ComputePressure, particlesThreadGroupCountX, 1, 1);

		if (Time.time >= nextTimeToUpdateHeat){
			nextTimeToUpdateHeat = Time.time + updateIntervalHeat;

			// ComputeHeat
			if (isFirstFrame){
				shader.SetBuffer(kernelID_ComputeHeat, shaderPropertyID_bins, bufferBins);
				shader.SetBuffer(kernelID_ComputeHeat, shaderPropertyID_particles, bufferParticles);
			}
			shader.Dispatch(kernelID_ComputeHeat, particlesThreadGroupCountX, 1, 1);

			// ApplyHeat
			if (isFirstFrame){
				shader.SetBuffer(kernelID_ApplyHeat, shaderPropertyID_bins, bufferBins);
				shader.SetBuffer(kernelID_ApplyHeat, shaderPropertyID_particles, bufferParticles);
			}
			shader.Dispatch(kernelID_ApplyHeat, particlesThreadGroupCountX, 1, 1);
		}

		// ComputeForces
		if (isFirstFrame){
			shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_particles, bufferParticles);
		}
		shader.Dispatch(kernelID_ComputeForces, particlesThreadGroupCountX, 1, 1);

		// Integrate
		if (isFirstFrame){
			shader.SetBuffer(kernelID_Integrate, shaderPropertyID_bins, bufferBins);
			shader.SetBuffer(kernelID_Integrate, shaderPropertyID_particles, bufferParticles);
			shader.SetTexture(kernelID_Integrate, shaderPropertyID_output, output);
		}
		shader.Dispatch(kernelID_Integrate, particlesThreadGroupCountX, 1, 1);

		// material.mainTexture = binsDirty;
		// material.mainTexture = binLoads;
		// material.mainTexture = bins_01_0;
		material.mainTexture = output;


		// bufferParticles.GetData(particles);
		// ParticleSystem.Particle[] unityParticles = new ParticleSystem.Particle[particles.Length];
		// int particleCount = particleSys.GetParticles(unityParticles);
		// for (int i = 0; i < unityParticles.Length; i++){
		// 	Particle particle = particles[i];
		// 	ParticleSystem.Particle unityParticle = unityParticles[i];

		// 	Vector2 worldPos = particle.Pos;
		// 	worldPos.x = worldPos.x / GRID_WIDTH_PIXELS * GRID_WIDTH_TILES;
		// 	worldPos.y = worldPos.y / GRID_HEIGHT_PIXELS * GRID_HEIGHT_TILES;
		// 	worldPos.x -= GRID_WIDTH_TILES * 0.5f;
		// 	worldPos.y -= GRID_HEIGHT_TILES * 0.5f;
		// 	unityParticle.position = worldPos;

		// 	Color color = Color.red;// Color.Lerp(Color.blue, Color.red, particle.Temperature / 1000.0f);
		// 	if (particleIndex == i){
		// 		color = Color.cyan;
		// 	}
		// 	else if (particle.Debug1 > 0){
		// 		color = Color.green;
		// 	}

		// 	color.a = particle.IsActive;
		// 	unityParticle.startColor = color;

		// 	unityParticles[i] = unityParticle;
		// }
		// particleSys.SetParticles(unityParticles, particleCount);

		// debugVars[0].Print();
		// PrintParticle();

		frame++;
		isFirstFrame = false;
	}
}