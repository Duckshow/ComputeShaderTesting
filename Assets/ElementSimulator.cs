using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementSimulator : MonoBehaviour {

	struct Particle{ // WARNING: variables must correspond to ElementSimulator.compute's Particle!
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
		// public float DebugTemp;
		// public float DebugThermal;
		public float Debug1;
		public float Debug2;
		public float Debug3;
		public float Debug4;
		// public float Debug5;
		// public float Debug6;
		// public float Debug7;
		// public float Debug8;
		// public float Debug9;
		// public float Debug10;
		// public float Debug11;
		// public float Debug12;
		// public float Debug13;

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

		public uint BinPosX;
		public uint BinPosY;
		public uint ElementIndex;

		public static int GetStride() {
			return sizeof(float) * 24/*35*/ + sizeof(uint) * 3; // must correspond to variables!
		}
	}

	// struct DebugVars{ // WARNING: variables must correspond to ElementSimulator.compute's Particle!
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

	// 	public static int GetStride() {
	// 		return sizeof(float) * 17; // must correspond to variables!
	// 	}

	// 	public void Print() {
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
	// 		Debug.Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
	// 	}
	// }

	private const int THREAD_COUNT_MAX = 1024;

	//#region[rgba(80, 0, 0, 1)] | WARNING: shared with ElementSimulator.compute! must be equal!
	private const int START_PARTICLE_COUNT = 32768; // must be divisible by THREAD_COUNT_X!
	private const int START_PARTICLE_COUNT_ACTIVE = 32768;

	private const int TEXTURE_AXIS_MAX = 16384;
	private static readonly int PARTICLE_TEXTURE_SIZE_X = Mathf.Min(START_PARTICLE_COUNT_ACTIVE, TEXTURE_AXIS_MAX);
	private static readonly int PARTICLE_TEXTURE_SIZE_Y = Mathf.Min((START_PARTICLE_COUNT_ACTIVE - (START_PARTICLE_COUNT_ACTIVE % PARTICLE_TEXTURE_SIZE_X)) / PARTICLE_TEXTURE_SIZE_X, TEXTURE_AXIS_MAX);

	private const int OUTPUT_THREAD_COUNT_X = 32;
	private const int OUTPUT_THREAD_COUNT_Y = 32;

	private const int BINS_THREAD_COUNT_X = 32;
	private const int BINS_THREAD_COUNT_Y = 32;

	private const int THREAD_COUNT_X = 64;
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 32;
	private const int GRID_HEIGHT_TILES = 32;
	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
	private const int BIN_SIZE = 8;
	private const int BIN_COUNT_X = GRID_WIDTH_PIXELS / BIN_SIZE;
	private const int BIN_COUNT_Y = GRID_HEIGHT_PIXELS / BIN_SIZE;
	private const int BIN_MAX_AMOUNT_OF_CONTENT = 12;
	private const int BIN_CLUSTER_SIZE = 5;
	private const int BIN_CLUSTER_CONTENT_MAX = BIN_CLUSTER_SIZE * BIN_MAX_AMOUNT_OF_CONTENT;
	//#endregion

	// kernels
	private const string KERNEL_INIT = "Init";
	private const string KERNEL_TESTWRITE = "TestWrite";
	private const string KERNEL_TESTREAD = "TestRead";
	private const string KERNEL_CLEAROUTPUTTEXTURE = "ClearOutputTexture";
	private const string KERNEL_CACHEPARTICLESINBINS = "CacheParticlesInBins";
	private const string KERNEL_COMPUTEDENSITY = "ComputeDensity";
	private const string KERNEL_COMPUTEPRESSURE = "ComputePressure";
	private const string KERNEL_COMPUTEHEAT = "ComputeHeat";
	private const string KERNEL_APPLYHEAT = "ApplyHeat";
	private const string KERNEL_COMPUTEFORCES = "ComputeForces";
	private const string KERNEL_INTEGRATE = "Integrate";
	private int kernelID_Init;
	private int kernelID_TestWrite;
	private int kernelID_TestRead;
	private int kernelID_ClearOutputTexture;
	private int kernelID_CacheParticlesInBins;
	private int kernelID_ComputeDensity;
	private int kernelID_ComputePressure;
	private int kernelID_ComputeHeat;
	private int kernelID_ApplyHeat;
	private int kernelID_ComputeForces;
	private int kernelID_Integrate;

	// properties
	private const string PROPERTY_PARTICLES = "particles";
	private const string PROPERTY_PARTICLECOUNT = "particleCount";
	// private const string PROPERTY_DEBUGBININDEX_X = "debugBinIndexX";
	// private const string PROPERTY_DEBUGBININDEX_Y = "debugBinIndexY";
	// private const string PROPERTY_DEBUGVARS = "debugVars";
	private const string PROPERTY_BINSDIRTY = "binsDirty";
	private const string PROPERTY_BINLOADS = "binLoads";
	private const string PROPERTY_BINS_00 = "bins_00";
	private const string PROPERTY_BINS_01 = "bins_01";
	private const string PROPERTY_BINS_02 = "bins_02";

	private const string PROPERTY_PARTICLESVELOCITYANDFORCE = "particlesVelocityAndForce";

	private const string PROPERTY_OUTPUT = "output";
	private const string PROPERTY_ISFIRSTFRAME = "isFirstFrame";

	private int shaderPropertyID_particles;
	private int shaderPropertyID_particleCount;
	// private int shaderPropertyID_debugBinIndexX;
	// private int shaderPropertyID_debugBinIndexY;
	// private int shaderPropertyID_debugVars;
	private int shaderPropertyID_binsDirty;
	private int shaderPropertyID_binLoads;
	private int shaderPropertyID_bins_00;
	private int shaderPropertyID_bins_01;
	private int shaderPropertyID_bins_02;

	private int shaderPropertyID_particlesVelocityAndForce;

	private int shaderPropertyID_output;
	private int shaderPropertyID_isFirstFrame;

	private float updateInterval = 0.0f;
	private float nextTimeToUpdate = 0.0f;

	private float updateIntervalBins= 0.1f;
	private float nextTimeToUpdateBins = 0.0f;

	private float updateIntervalHeat = 0.5f;
	private float nextTimeToUpdateHeat = 0.0f;

	private ComputeBuffer bufferParticles;
	private Particle[] particles;

	// private ComputeBuffer bufferDebug;
	//	private DebugVars[] debugVars;

	private RenderTexture binsDirty;
	private RenderTexture binLoads;
	private RenderTexture bins_00;
	private RenderTexture bins_01;
	private RenderTexture bins_02;

	private RenderTexture particlesVelocityAndForce;

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
		// Debug.Log("Debug5 = " + particles[particleIndex].Debug5);
		// Debug.Log("Debug6 = " + particles[particleIndex].Debug6);
		// Debug.Log("Debug7 = " + particles[particleIndex].Debug7);
		// Debug.Log("Debug8 = " + particles[particleIndex].Debug8);
		// Debug.Log("Debug9 = " + particles[particleIndex].Debug9);
		// Debug.Log("Debug10 = " + particles[particleIndex].Debug10);
		// Debug.Log("Debug11 = " + particles[particleIndex].Debug11);
		// Debug.Log("Debug12 = " + particles[particleIndex].Debug12);
		Debug.Log("=====================================");
	}

	void OnValidate() { 
		if (particles == null || particles.Length == 0) return;

		ParticleSystem.Particle[] unityParticles = new ParticleSystem.Particle[particles.Length];
		int particleCount = particleSys.GetParticles(unityParticles);
		for (int i = 0; i < unityParticles.Length; i++){
			Particle particle = particles[i];
			ParticleSystem.Particle unityParticle = unityParticles[i];

			Color color = Color.Lerp(Color.blue, Color.red, particle.Temperature / 1000.0f);
			if (particleIndex == i){
				color = Color.cyan;
			}
			// else if (particle.DebugTemp > 0){
			// 	color = Color.green;
			// }

			color.a = particle.IsActive;
			unityParticle.startColor = color;

			unityParticles[i] = unityParticle;
		}
		particleSys.SetParticles(unityParticles, particleCount);
	}

	void Awake(){
		kernelID_Init = shader.FindKernel(KERNEL_INIT);
		kernelID_TestWrite = shader.FindKernel(KERNEL_TESTWRITE);
		kernelID_TestRead = shader.FindKernel(KERNEL_TESTREAD);
		kernelID_ClearOutputTexture = shader.FindKernel(KERNEL_CLEAROUTPUTTEXTURE);
		kernelID_CacheParticlesInBins = shader.FindKernel(KERNEL_CACHEPARTICLESINBINS);
		kernelID_ComputeDensity = shader.FindKernel(KERNEL_COMPUTEDENSITY);
		kernelID_ComputePressure = shader.FindKernel(KERNEL_COMPUTEPRESSURE);
		kernelID_ComputeHeat = shader.FindKernel(KERNEL_COMPUTEHEAT);
		kernelID_ApplyHeat = shader.FindKernel(KERNEL_APPLYHEAT);
		kernelID_ComputeForces = shader.FindKernel(KERNEL_COMPUTEFORCES);
		kernelID_Integrate = shader.FindKernel(KERNEL_INTEGRATE);

		shaderPropertyID_particles = Shader.PropertyToID(PROPERTY_PARTICLES);
		shaderPropertyID_particleCount = Shader.PropertyToID(PROPERTY_PARTICLECOUNT);
		// shaderPropertyID_debugBinIndexX = Shader.PropertyToID(PROPERTY_DEBUGBININDEX_X);
		// shaderPropertyID_debugBinIndexY = Shader.PropertyToID(PROPERTY_DEBUGBININDEX_Y);
		//		shaderPropertyID_debugVars = Shader.PropertyToID(PROPERTY_DEBUGVARS);
		shaderPropertyID_binsDirty = Shader.PropertyToID(PROPERTY_BINSDIRTY);
		shaderPropertyID_binLoads = Shader.PropertyToID(PROPERTY_BINLOADS);
		shaderPropertyID_bins_00 = Shader.PropertyToID(PROPERTY_BINS_00);
		shaderPropertyID_bins_01 = Shader.PropertyToID(PROPERTY_BINS_01);
		shaderPropertyID_bins_02 = Shader.PropertyToID(PROPERTY_BINS_02);

		shaderPropertyID_particlesVelocityAndForce = Shader.PropertyToID(PROPERTY_PARTICLESVELOCITYANDFORCE);

		shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
		shaderPropertyID_isFirstFrame = Shader.PropertyToID(PROPERTY_ISFIRSTFRAME);
	}

	void OnDisable(){
		bufferParticles.Dispose();
		//bufferDebug.Dispose();
	}
	
	void Start () {
		InitShader();
		particleSys.Play();
		particleSys.Emit(particles.Length);
	}

	void InitShader(){
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		particles = new Particle[START_PARTICLE_COUNT];
		// debugVars = new DebugVars[1];

		binsDirty = new RenderTexture(BIN_COUNT_X, BIN_COUNT_Y, 0, RenderTextureFormat.R8);
		binsDirty.enableRandomWrite = true;
		binsDirty.filterMode = FilterMode.Point;
		binsDirty.wrapMode = TextureWrapMode.Clamp;
		binsDirty.Create();
		
		binLoads = new RenderTexture(BIN_COUNT_X, BIN_COUNT_Y, 0, RenderTextureFormat.R8);
		binLoads.enableRandomWrite = true;
		binLoads.filterMode = FilterMode.Point;
		binLoads.wrapMode = TextureWrapMode.Clamp;
		binLoads.Create();

		// particlesDensityPressureTemperatureAndRepel = new RenderTexture(PARTICLE_TEXTURE_SIZE_X, PARTICLE_TEXTURE_SIZE_Y, 0, RenderTextureFormat.ARGB32);
		// particlesDensityPressureTemperatureAndRepel.enableRandomWrite = true;
		// particlesDensityPressureTemperatureAndRepel.filterMode = FilterMode.Point;
		// particlesDensityPressureTemperatureAndRepel.wrapMode = TextureWrapMode.Clamp;
		// particlesDensityPressureTemperatureAndRepel.Create();

		// particlesHeatExchange = new RenderTexture(PARTICLE_TEXTURE_SIZE_X, PARTICLE_TEXTURE_SIZE_Y, 0, RenderTextureFormat.ARGB32);
		// particlesHeatExchange.enableRandomWrite = true;
		// particlesHeatExchange.filterMode = FilterMode.Point;
		// particlesHeatExchange.wrapMode = TextureWrapMode.Clamp;
		// particlesHeatExchange.Create();

		// particlesHeatIndices = new RenderTexture(PARTICLE_TEXTURE_SIZE_X, PARTICLE_TEXTURE_SIZE_Y, 0, RenderTextureFormat.ARGB32);
		// particlesHeatIndices.enableRandomWrite = true;
		// particlesHeatIndices.filterMode = FilterMode.Point;
		// particlesHeatIndices.wrapMode = TextureWrapMode.Clamp;
		// particlesHeatIndices.Create();

		// particlesBins = new RenderTexture(PARTICLE_TEXTURE_SIZE_X, PARTICLE_TEXTURE_SIZE_Y, 0, RenderTextureFormat.ARGB32);
		// particlesBins.enableRandomWrite = true;
		// particlesBins.filterMode = FilterMode.Point;
		// particlesBins.wrapMode = TextureWrapMode.Clamp;
		// particlesBins.Create();


		bins_00 = new RenderTexture(BIN_COUNT_X, BIN_COUNT_Y, 0, RenderTextureFormat.RGBAUShort);
		bins_00.enableRandomWrite = true;
		bins_00.filterMode = FilterMode.Point;
		bins_00.wrapMode = TextureWrapMode.Clamp;
		bins_00.Create();

		bins_01 = new RenderTexture(BIN_COUNT_X, BIN_COUNT_Y, 0, RenderTextureFormat.RGBAUShort);
		bins_01.enableRandomWrite = true;
		bins_01.filterMode = FilterMode.Point;
		bins_01.wrapMode = TextureWrapMode.Clamp;
		bins_01.Create();
		
		bins_02 = new RenderTexture(BIN_COUNT_X, BIN_COUNT_Y, 0, RenderTextureFormat.RGBAUShort);
		bins_02.enableRandomWrite = true;
		bins_02.filterMode = FilterMode.Point;
		bins_02.wrapMode = TextureWrapMode.Clamp;
		bins_02.Create();

		particlesVelocityAndForce = new RenderTexture(PARTICLE_TEXTURE_SIZE_X, PARTICLE_TEXTURE_SIZE_Y, 0, RenderTextureFormat.ARGBFloat);
		particlesVelocityAndForce.enableRandomWrite = true;
		particlesVelocityAndForce.filterMode = FilterMode.Point;
		particlesVelocityAndForce.wrapMode = TextureWrapMode.Clamp;
		particlesVelocityAndForce.Create();

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
					if (y >= GRID_HEIGHT_PIXELS){
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
			//particle.Temperature = Random.Range(0, 1000);
			//particle.Temperature = x < GRID_WIDTH_PIXELS * 0.5f ? 10000 : 0;
			particle.Temperature = reverse ? 200 : 300;
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
		bufferParticles = new ComputeBuffer(particles.Length, Particle.GetStride());
		// bufferDebug = new ComputeBuffer(1, DebugVars.GetStride());

		// Init
		bufferParticles.SetData(particles);
		shader.SetBuffer(kernelID_Init, shaderPropertyID_particles, bufferParticles);
		shader.SetInt(shaderPropertyID_particleCount, START_PARTICLE_COUNT_ACTIVE);
		shader.Dispatch(kernelID_Init, Mathf.CeilToInt(particles.Length / THREAD_COUNT_X), 1, 1);
		bufferParticles.GetData(particles);
	}

	void Update() {
		if (Time.time < nextTimeToUpdate) return;
		nextTimeToUpdate = Time.time + updateInterval;
		UpdateShader();
	}

	void UpdateShader() {
		int particlesThreadGroupCountX = Mathf.CeilToInt(particles.Length / THREAD_COUNT_X);
		int outputThreadGroupCountX = Mathf.CeilToInt(GRID_WIDTH_PIXELS / OUTPUT_THREAD_COUNT_X);
		int outputThreadGroupCountY = Mathf.CeilToInt(GRID_HEIGHT_PIXELS / OUTPUT_THREAD_COUNT_Y);
		int binsThreadGroupCountX = Mathf.CeilToInt(BIN_COUNT_X / BINS_THREAD_COUNT_X);
		int binsThreadGroupCountY = Mathf.CeilToInt(BIN_COUNT_Y / BINS_THREAD_COUNT_Y);
		
		shader.SetFloat(shaderPropertyID_isFirstFrame, isFirstFrame ? 1.0f : 0.0f);
		// shader.SetInt(shaderPropertyID_debugBinIndexX, (int)debugBinIndex.x);
		// shader.SetInt(shaderPropertyID_debugBinIndexY, (int)debugBinIndex.y);

		// bufferParticles.SetData(particles);
		// shader.SetBuffer(kernelID_TestWrite, shaderPropertyID_particles, bufferParticles);
		// shader.SetTexture(kernelID_TestWrite, shaderPropertyID_particlesVelocityAndForce, particlesVelocityAndForce);
		// shader.Dispatch(kernelID_TestWrite, particlesThreadGroupCountX, 1, 1);

		// bufferParticles.SetData(particles);
		// shader.SetBuffer(kernelID_TestRead, shaderPropertyID_particles, bufferParticles);
		// shader.SetTexture(kernelID_TestRead, shaderPropertyID_particlesVelocityAndForce, particlesVelocityAndForce);
		// shader.Dispatch(kernelID_TestRead, particlesThreadGroupCountX, 1, 1);

		// ClearOutputTexture
		shader.SetTexture(kernelID_ClearOutputTexture, shaderPropertyID_output, output);
		shader.Dispatch(kernelID_ClearOutputTexture, outputThreadGroupCountX, outputThreadGroupCountY, 1);

		if (Time.time >= nextTimeToUpdateBins) { 
			nextTimeToUpdateBins = Time.time + updateIntervalBins;
	
			// CacheParticlesInBins
			bufferParticles.SetData(particles);
			shader.SetBuffer(kernelID_CacheParticlesInBins, shaderPropertyID_particles, bufferParticles);
			// bufferDebug.SetData(debugVars);
			// shader.SetBuffer(kernelID_CacheParticlesInBins, shaderPropertyID_debugVars, bufferDebug);
			shader.SetTexture(kernelID_CacheParticlesInBins, shaderPropertyID_binsDirty, binsDirty);
			shader.SetTexture(kernelID_CacheParticlesInBins, shaderPropertyID_binLoads, binLoads);
			shader.SetTexture(kernelID_CacheParticlesInBins, shaderPropertyID_bins_00, bins_00);
			shader.SetTexture(kernelID_CacheParticlesInBins, shaderPropertyID_bins_01, bins_01);
			shader.SetTexture(kernelID_CacheParticlesInBins, shaderPropertyID_bins_02, bins_02);
			//shader.SetInt(shaderPropertyID_particleCount, particles.Length);
			shader.Dispatch(kernelID_CacheParticlesInBins, binsThreadGroupCountX, binsThreadGroupCountY, 1);
			bufferParticles.GetData(particles);
			// bufferDebug.GetData(debugVars);
		}

		// ComputeDensity
		// bufferDebug.SetData(debugVars);
		bufferParticles.SetData(particles);
		shader.SetBuffer(kernelID_ComputeDensity, shaderPropertyID_particles, bufferParticles);
		shader.SetTexture(kernelID_ComputeDensity, shaderPropertyID_binLoads, binLoads);
		shader.SetTexture(kernelID_ComputeDensity, shaderPropertyID_bins_00, bins_00);
		shader.SetTexture(kernelID_ComputeDensity, shaderPropertyID_bins_01, bins_01);
		shader.SetTexture(kernelID_ComputeDensity, shaderPropertyID_bins_02, bins_02);
		shader.Dispatch	(kernelID_ComputeDensity, particlesThreadGroupCountX, 1, 1);

		// ComputePressure
		shader.SetTexture(kernelID_ComputePressure, shaderPropertyID_binLoads, binLoads);
		shader.SetTexture(kernelID_ComputePressure, shaderPropertyID_bins_00, bins_00);
		shader.SetTexture(kernelID_ComputePressure, shaderPropertyID_bins_01, bins_01);
		shader.SetTexture(kernelID_ComputePressure, shaderPropertyID_bins_02, bins_02);
		shader.Dispatch(kernelID_ComputePressure, particlesThreadGroupCountX, 1, 1);

		// bufferDebug.GetData(debugVars);

		if (Time.time >= nextTimeToUpdateHeat){
			nextTimeToUpdateHeat = Time.time + updateIntervalHeat;
			
			shader.SetTexture(kernelID_ComputeHeat, shaderPropertyID_binLoads, binLoads);
			shader.SetTexture(kernelID_ComputeHeat, shaderPropertyID_bins_00, bins_00);
			shader.SetTexture(kernelID_ComputeHeat, shaderPropertyID_bins_01, bins_01);
			shader.SetTexture(kernelID_ComputeHeat, shaderPropertyID_bins_02, bins_02);
			shader.Dispatch(kernelID_ComputeHeat, particlesThreadGroupCountX, 1, 1);

			shader.SetTexture(kernelID_ApplyHeat, shaderPropertyID_binLoads, binLoads);
			shader.SetTexture(kernelID_ApplyHeat, shaderPropertyID_bins_00, bins_00);
			shader.SetTexture(kernelID_ApplyHeat, shaderPropertyID_bins_01, bins_01);
			shader.SetTexture(kernelID_ApplyHeat, shaderPropertyID_bins_02, bins_02);
			shader.Dispatch(kernelID_ApplyHeat, particlesThreadGroupCountX, 1, 1);
		}

		// bufferParticles.SetData(particles);
		// shader.SetBuffer(kernelID_ComputeForces, shaderPropertyID_particles, bufferParticles);

		// ComputeForces
		shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_binLoads, binLoads);
		shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_bins_00, bins_00);
		shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_bins_01, bins_01);
		shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_bins_02, bins_02);
		shader.SetTexture(kernelID_ComputeForces, shaderPropertyID_particlesVelocityAndForce, particlesVelocityAndForce);
		shader.Dispatch(kernelID_ComputeForces, particlesThreadGroupCountX, 1, 1);

		// Integrate
		// bufferDebug.SetData(debugVars);
		// shader.SetBuffer(kernelID_Integrate, shaderPropertyID_debugVars, bufferDebug);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_binsDirty, binsDirty);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_binLoads, binLoads);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_bins_00, bins_00);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_bins_01, bins_01);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_bins_02, bins_02);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_particlesVelocityAndForce, particlesVelocityAndForce);
		shader.SetTexture(kernelID_Integrate, shaderPropertyID_output, output);
		shader.Dispatch(kernelID_Integrate, particlesThreadGroupCountX, 1, 1);
		bufferParticles.GetData(particles);
		// bufferDebug.GetData(debugVars);

		//material.mainTexture = binsDirty;
		//material.mainTexture = binLoads;
		//material.mainTexture = bins_00;
		// material.mainTexture = output;


		ParticleSystem.Particle[] unityParticles = new ParticleSystem.Particle[particles.Length];
		int particleCount = particleSys.GetParticles(unityParticles);
		for (int i = 0; i < unityParticles.Length; i++){
			Particle particle = particles[i];
			ParticleSystem.Particle unityParticle = unityParticles[i];

			Vector2 worldPos = particle.Pos;
			worldPos.x = worldPos.x / GRID_WIDTH_PIXELS * GRID_WIDTH_TILES;
			worldPos.y = worldPos.y / GRID_HEIGHT_PIXELS * GRID_HEIGHT_TILES;
			worldPos.x -= GRID_WIDTH_TILES * 0.5f;
			worldPos.y -= GRID_HEIGHT_TILES * 0.5f;
			unityParticle.position = worldPos;

			Color color = Color.Lerp(Color.blue, Color.red, particle.Temperature / 1000.0f);
			if (particleIndex == i){
				color = Color.cyan;
			}
			// else if (particle.DebugTemp > 0){
			// 	color = Color.green;
			// }

			color.a = particle.IsActive;
			unityParticle.startColor = color;

			unityParticles[i] = unityParticle;
		}
		particleSys.SetParticles(unityParticles, particleCount);

		// debugVars[0].Print();
		//PrintParticle();

		isFirstFrame = false;
	}
}